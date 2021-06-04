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
        public const string Net6XamarinAndroid = "net6.0-android";
        public const string Net6XamariniOS = "net6.0-ios";

        public static ImmutableArray<string> KnownXamarinReferences => ImmutableArray.Create(
          "Mono.Android",
          "Xamarin.iOS"
      );

        public static ImmutableArray<Guid> XamarinDroidProjectTypeGuids => ImmutableArray.Create(
            Guid.Parse("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}") // Xamarin.Android
        );

        public static ImmutableArray<Guid> XamariniOSProjectTypeGuids => ImmutableArray.Create(
          Guid.Parse("{FEACFBD2-3405-455C-9665-78FE426C6842}") // Xamarin.iOS
      );
    }
}
