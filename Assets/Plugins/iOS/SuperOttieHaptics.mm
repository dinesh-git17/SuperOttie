// Taptic feedback for gameplay events. Called from Haptics.cs through P/Invoke.
#import <UIKit/UIKit.h>

extern "C" void SuperOttie_Haptic(int style)
{
    UIImpactFeedbackStyle s = style <= 0 ? UIImpactFeedbackStyleLight
                            : style == 1 ? UIImpactFeedbackStyleMedium
                                         : UIImpactFeedbackStyleHeavy;
    UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    [generator impactOccurred];
}
