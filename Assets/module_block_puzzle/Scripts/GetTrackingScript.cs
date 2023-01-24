using System;
using System.Collections;
using System.Collections.Generic;
using Sonat;
using UnityEngine;

namespace BlockPuzzle
{
    public class GetTrackingScript : CustomGetTrackingScript
    {
        public override IEnumerable<LogParameter> GetAdmobLog(int step, IEnumerable<LogParameter> input, AdTypeLog adType)
        {
            yield return new LogParameter("format", adType.ToString());
            
            if (step == 10
                || step == 11
                || step == 12
                || step == 13
            )
            {
                if(adType == AdTypeLog.banner)
                    yield return new LogParameter("ad_placement", RootView.rootView.screenRoot.CurrentScreen.ScreenName);
                if(adType == AdTypeLog.interstitial)
                    yield return new LogParameter("ad_placement", SonatAnalyticTracker.InterstitialLogName);
                if(adType == AdTypeLog.rewarded_video)
                    yield return new LogParameter("ad_placement", SonatAnalyticTracker.RewardedLogName);
            }

            if (step == 12)
            {
                if(adType == AdTypeLog.interstitial)
                    yield return new LogParameter("ad_duration", Time.unscaledTime - Kernel.Resolve<AdsManager>().TimeStartInters);
                if(adType == AdTypeLog.rewarded_video)
                    yield return new LogParameter("ad_duration", Time.unscaledTime - Kernel.Resolve<AdsManager>().TimeStartVideo);
            }
            
            foreach (var logParameter in input)
            {
                yield return logParameter;
            }
        }
    }
}
