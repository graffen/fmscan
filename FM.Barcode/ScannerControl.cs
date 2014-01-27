using Microsoft.Devices;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ZXing;

namespace FM.Barcode
{
    [TemplateVisualState(Name=ResultFoundIndicatorState, GroupName=ResultIndicatorStates)]
    [TemplateVisualState(Name = ResultNotFoundIndicatorState, GroupName = ResultIndicatorStates)]
    [TemplatePart(Name=_videoPreviewBrushName, Type=typeof(VideoBrush))]
    public class ScannerControl : Control
    {
        #region GlyphHeight

        public Double GlyphHeight
        {
            get { return (Double)GetValue(GlyphHeightProperty); }
            set { SetValue(GlyphHeightProperty, value); }
        }

        public static readonly DependencyProperty GlyphHeightProperty =
            DependencyProperty.Register("GlyphHeight", typeof(Double), typeof(ScannerControl), new PropertyMetadata(300.0)); 

        #endregion

        #region GlyphWidth

        public Double GlyphWidth
        {
            get { return (Double)GetValue(GlyphWidthProperty); }
            set { SetValue(GlyphWidthProperty, value); }
        }

        public static readonly DependencyProperty GlyphWidthProperty =
            DependencyProperty.Register("GlyphWidth", typeof(Double), typeof(ScannerControl), new PropertyMetadata(300.0)); 

        #endregion

        #region GlyphFill

        public Brush GlyphFill
        {
            get { return (Brush)GetValue(GlyphFillProperty); }
            set { SetValue(GlyphFillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GlyphFill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GlyphFillProperty =
            DependencyProperty.Register("GlyphFill", typeof(Brush), typeof(ScannerControl), new PropertyMetadata(new SolidColorBrush(Colors.White))); 

        #endregion

        #region SuccessGlyphFill

        public Brush SuccessGlyphFill
        {
            get { return (Brush)GetValue(SuccessGlyphFillProperty); }
            set { SetValue(SuccessGlyphFillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SuccessGlyphFill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SuccessGlyphFillProperty =
            DependencyProperty.Register("SuccessGlyphFill", typeof(Brush), typeof(ScannerControl), new PropertyMetadata(new SolidColorBrush(Colors.Green)));

        

        #endregion

        #region PhotoCamera

        PhotoCamera _currentCamera;
        public PhotoCamera PhotoCamera { get { return _currentCamera; } } 

        #endregion

        #region ScanInterval And timer Handling

        TimeSpan _ScanInterval = TimeSpan.FromMilliseconds(250);
        public TimeSpan ScanInterval
        {
            get
            {
                return _ScanInterval;
            }
            set
            {
                _ScanInterval = value;
                BuildTimer();
            }
        }

        private void BuildTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            _timer = new DispatcherTimer();
            _timer.Interval = _ScanInterval;
            _timer.Tick += (timer, args) => CaptureFrame();
        }

        #endregion

        #region Visual states

        private const string ResultIndicatorStates = "ResultIndicatorStates";

        private const string ResultNotFoundIndicatorState = "ResultNotFound";
        private const string ResultFoundIndicatorState = "ResultFound";

        #endregion

        #region IBarcodeReader

        IBarcodeReader _barcodeReader;
        public IBarcodeReader BarcodeReader
        {
            get { return _barcodeReader; }
        }

        #endregion

        #region Public Custom Events

        public event EventHandler<ScannerResultEventArgs> ScanResultFound;

        #endregion

        #region Private Events

        private static event EventHandler ReloadCamera; 

        #endregion

        #region Required elements Names

        private const String _videoPreviewBrushName = "previewVideoBrush";

        #endregion

        private VideoBrush _videoPreviewBrush;
        private CameraType _cameraType = CameraType.Primary;
        private DispatcherTimer _timer; 
        /// <summary>
        /// Stores a reference to the current root visual.
        /// </summary>
        private PhoneApplicationFrame _rootVisual;
        
        public ScannerControl()
        {
            DefaultStyleKey = typeof(ScannerControl);

            if (null == Application.Current.RootVisual)
            {
                //To soon to initialize the main layout so we wait until LayoutUpdated event
                LayoutUpdated += OnLayoutUpdated;
            }
            else
            {
                InitializeRootVisual();
            }
            Unloaded += ScannerControl_Unloaded;
            Loaded += ScannerControl_Loaded;
            Tap += ScannerControl_Tap;
        }

        void ScannerControl_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if(_currentCamera != null)
            {
                try
                {
                    if (_currentCamera.IsFocusAtPointSupported)
                    {
                        var pt = e.GetPosition(this);
                        double x = pt.X / this.ActualWidth;
                        double y = pt.Y / this.ActualHeight;
                        _currentCamera.FocusAtPoint(x, y);
                    }
                    else if (_currentCamera.IsFocusSupported)
                    {
                        _currentCamera.Focus();
                    }
                }
                catch (Exception)
                {
                    //for many reason focus may fail son we catch the error quietly
                }
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            if (null != Application.Current.RootVisual)
            {
                InitializeRootVisual();
                // Unhook event since not needed anymore
                LayoutUpdated -= OnLayoutUpdated;
            }
        }

        public override void OnApplyTemplate()
        {
            _videoPreviewBrush = this.GetTemplateChild(_videoPreviewBrushName) as VideoBrush;

            //do not perfom heavy initialization in design mode
            if (!DesignerProperties.IsInDesignTool)
            {
                LoadCameraBrush();
            }
            base.OnApplyTemplate();
        }

        /// <summary>
        /// Initialize the _rootVisual property (if possible and not already done).
        /// </summary>
        private void InitializeRootVisual()
        {
            if (null == _rootVisual)
            {
                // Try to capture the Application's RootVisual
                _rootVisual = Application.Current.RootVisual as PhoneApplicationFrame;
                if (null != _rootVisual)
                {
                    _rootVisual.OrientationChanged -= OnOrientationChanged;
                    _rootVisual.OrientationChanged += OnOrientationChanged;

                    if (_videoPreviewBrush != null)
                    {
                        ApplyOrientation(_rootVisual.Orientation);
                    }
                }
            }
        }

        private void OnOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            ApplyOrientation(e.Orientation);
        }

        private void ApplyOrientation(PageOrientation pageOrientation)
        {
            // Protait rotation when camera is on back of phone.
            int videoRotation = 90;
        
            switch (pageOrientation)
            {
                case PageOrientation.Landscape:
                case PageOrientation.LandscapeLeft:
                    videoRotation = 0;
                    break;
                case PageOrientation.LandscapeRight:
                    videoRotation = 180;
                    break;
                case PageOrientation.Portrait:
                case PageOrientation.PortraitUp:
                    videoRotation = 90;
                    break;
                case PageOrientation.PortraitDown:
                    videoRotation = 270;
                    break;
            }

            //When using front camera the oriention must inverted
            if (_currentCamera != null && _currentCamera.CameraType == CameraType.FrontFacing) videoRotation *= -1;

            // Rotate video brush from camera.
            _videoPreviewBrush.RelativeTransform =
                new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = videoRotation };
        }

        private void LoadCameraBrush()
        {
            if (_currentCamera != null)
                return; //camera already initialized

            //find the other camera if the prefered camera  is not available (usualy Primary Camera is always available)
            CameraType invertCameraType = _cameraType == CameraType.FrontFacing ? CameraType.Primary : CameraType.FrontFacing;
            CameraType targetType = PhotoCamera.IsCameraTypeSupported(_cameraType)? _cameraType : invertCameraType;

            ReloadCamera -= ScannerControl_ReloadCamera;
            ReloadCamera += ScannerControl_ReloadCamera;

            //Create the photo camera only if supported
            if (PhotoCamera.IsCameraTypeSupported(targetType))
            {
                //Create the timer that will capture camera frame
                BuildTimer();

                //Create the camera object
                _currentCamera = new PhotoCamera(targetType);
                _currentCamera.Initialized += _currentCamera_Initialized;
                _videoPreviewBrush.SetSource(_currentCamera);
                if (_rootVisual != null)
                {
                    ApplyOrientation(_rootVisual.Orientation);
                }
                
            }
        }

        void ScannerControl_ReloadCamera(object sender, EventArgs e)
        {
            if (_timer != null && _timer.IsEnabled)
                _timer.Stop();
            DisposeCamera();
            LoadCameraBrush();

        }

        public void SetPreferedCameraType(CameraType type)
        {
            if (PhotoCamera.IsCameraTypeSupported(type))
            {
                _cameraType = type;
                _currentCamera.Dispose();
                _currentCamera = null;
                LoadCameraBrush();
            }
        }

        void _currentCamera_Initialized(object sender, CameraOperationCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    //Start the timer that should always be initialiazed before the camera
                    //once the camera ready we start the timer
                    ResumeScan();
                });
            
        }

        void ScannerControl_Loaded(object sender, RoutedEventArgs e)
        {
            _barcodeReader = new BarcodeReader();
            _barcodeReader.Options.TryHarder = true;

            LoadCameraBrush();
        }

        void ScannerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeCamera();

            //make sure to unregister to every outside events
            _rootVisual.OrientationChanged -= OnOrientationChanged;
            ReloadCamera -= ScannerControl_ReloadCamera;
            
        }

        private void DisposeCamera()
        {
            //Dispose the camera
            if (_currentCamera != null)
            {
                _currentCamera.Dispose();
                _currentCamera = null;
            }
        }

        /// <summary>
        /// Get the Scanner result and process it
        /// </summary>
        private void CaptureFrame()
        {
            Result rsl = AnalyseFrame();
            if (rsl != null)
            {
                //go to the visual state
                VisualStateManager.GoToState(this, ResultFoundIndicatorState, false);

                //if result is not null then ZXing found a match
                if(ScanResultFound != null)
                {
                    //Notify all listeners that we have found something
                    ScannerResultEventArgs args = new ScannerResultEventArgs(rsl);
                    ScanResultFound(this, args);
                }
            }
            else
            {
                //go to the visual state
                VisualStateManager.GoToState(this, ResultNotFoundIndicatorState, false);
            }
        }

        private Result AnalyseFrame()
        {

            if (_currentCamera == null)
                return null;

            var width = Convert.ToInt32(_currentCamera.PreviewResolution.Width);
            var height = Convert.ToInt32(_currentCamera.PreviewResolution.Height);
            var previewBuffer = new byte[width * height];

            _currentCamera.GetPreviewBufferY(previewBuffer);

            Result result = _barcodeReader.Decode(previewBuffer, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
            return result;
        }

        /// <summary>
        /// Stop barcode Scanning
        /// </summary>
        public void StopScan()
        {
            if (_timer != null)
                _timer.Stop();
        }

        /// <summary>
        /// Resume Barcode scanning
        /// </summary>
        public void ResumeScan()
        {
            if (_timer != null && !_timer.IsEnabled)
                _timer.Start();
        }

        public static void ReloadComponents()
        {
            if(ReloadCamera != null)
                ReloadCamera(null, EventArgs.Empty);
        }
    }
}
