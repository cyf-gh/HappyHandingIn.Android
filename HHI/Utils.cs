using Android.App;
using Android.Content;
using Android.Support.V7.App;
using Android.Views.InputMethods;

namespace stLibCS {
    namespace Android {
        static public class AgileFoo {
            static public bool HideInput( Context context ) {
                InputMethodManager inputMethodManager = (InputMethodManager)context.GetSystemService( Activity.InputMethodService );
                inputMethodManager.HideSoftInputFromWindow( ( (AppCompatActivity)context ).CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways );
                return true;
            }
        }
    }
}