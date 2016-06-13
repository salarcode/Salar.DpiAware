##Salar.DpiAware
High DPI Per-Monitor Windows Forms

Automatically updates forms respecting current monitors' DPI.

The original work is thanks to emoacht: 
[https://emoacht.wordpress.com/2013/10/30/per-monitor-dpi-aware-in-windows-forms/](https://emoacht.wordpress.com/2013/10/30/per-monitor-dpi-aware-in-windows-forms/)

###[NuGet Package](https://www.nuget.org/packages/Salar.DpiAware/)
```
PM> Install-Package Salar.DpiAware
```

###How to enable DPI-Aware in your application.
For now read this page: [Per-Monitor DPI Aware in Windows Forms](https://emoacht.wordpress.com/2013/10/30/per-monitor-dpi-aware-in-windows-forms/)

###How to use

Just change the base class of your form to Salar.HDpiForm and you're done!

```csharp
public partial class frmSampleForm : Salar.HDpiForm
{
    ...
```

There is also an event named `OnDpiChange` which will be fired when after a DPI change is detected and applied.
