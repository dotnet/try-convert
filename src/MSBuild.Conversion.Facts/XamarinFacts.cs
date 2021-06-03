using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuild.Conversion.Facts
{
    public static class XamarinFacts
    {
        public const string XamarinAndroidPropertiesName = "AndroidApplication";
        public const string XamariniOSPropertiesName = "MtouchEnableSGenConc";

        public static ImmutableArray<Guid> XamarinProjectTypeGuids => ImmutableArray.Create(
            Guid.Parse("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"), // Xamarin.Android
            Guid.Parse("{FEACFBD2-3405-455C-9665-78FE426C6842}"), // Xamarin.iOS
            Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") // Xamarin.Forms
        );
    }
}
