using System;
using System.Collections;
using System.Collections.Generic;
using Sonat;
using UnityEngine;

namespace SpaceShooter
{
    public class GetTrackingScript : CustomGetTrackingScript
    {
        public override IEnumerable<LogParameter> GetAdmobLog(int step, IEnumerable<LogParameter> input, AdType adType)
        {
            yield return new LogParameter("format", adType.ToString());
            
            if (step == 10
                || step == 11
                || step == 12
                || step == 13
            )
            {
                if(adType == AdType.banner)
                    yield return new LogParameter("ad_placement", RootView.rootView.screenRoot.CurrentScreen.placementName);
                if(adType == AdType.interstital)
                    yield return new LogParameter("ad_placement", SonatAnalyticTracker.InterstitialLogName);
                if(adType == AdType.rewarded_video)
                    yield return new LogParameter("ad_placement", SonatAnalyticTracker.RewardedLogName);
            }

            if (step == 12)
            {
                if(adType == AdType.interstital)
                    yield return new LogParameter("ad_duration", Time.unscaledTime - Kernel.Resolve<AdsManager>().TimeStartInters);
                if(adType == AdType.rewarded_video)
                    yield return new LogParameter("ad_duration", Time.unscaledTime - Kernel.Resolve<AdsManager>().TimeStartVideo);
            }
            
            foreach (var logParameter in input)
            {
                yield return logParameter;
            }
        }
    }
}
