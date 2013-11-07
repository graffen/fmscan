fmscan
======

*Barcode Reader control for Windows Phone 8* is a ready to use XAML control that allow you to read barcodes and QR codes.  
This control is an implementation of [Zxing.net](http://zxingnet.codeplex.com/).

In order to use the control install it via nuget.
`PM> Install-Package FM.Barcode`

Then include the following namespace in your XAML file.
`xmlns:fm="clr-namespace:FM.Barcode;assembly=FM.Barcode"`

Once done you can use the scanner control in your page
`<fm:ScannerControl x:Name="ScanControl" />`  
  
In order to support app switching please insert the following code in your method *Application_Activated* located in your App.xaml.cs
`private void Application_Activated(object sender, ActivatedEventArgs e)
{
    FM.Barcode.ScannerControl.ReloadComponents();
}
`
