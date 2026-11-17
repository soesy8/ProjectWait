using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // No height offset is committed until wear/confirmation and pose stability agree.
    public sealed class ElevatorXRHeightCalibration
    {
        public bool Pending {get;private set;}=true;
        public bool Calibrated {get;private set;}
        public float StableSeconds {get;private set;}
        bool confirmed,wasPresent;
        float referenceHeight;
        public void Request(bool confirm=false)
        {
            Pending=true;Calibrated=false;confirmed=confirm;StableSeconds=0;
        }
        public void AcceptTransferredHeight()
        {
            Pending=false;Calibrated=true;confirmed=false;wasPresent=true;StableSeconds=0;
        }
        public bool Observe(float height,bool tracked,bool? present,float delta)
        {
            if(present==false)
            {
                wasPresent=false;Request();return false;
            }
            if(present==true&&!wasPresent){wasPresent=true;Request();}
            if(!Pending)return false;
            if(!tracked||present!=true&&!confirmed||float.IsNaN(height)||float.IsInfinity(height)||height<.45f||height>2.5f||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))
            {StableSeconds=0;return false;}
            if(StableSeconds==0||Mathf.Abs(height-referenceHeight)>.025f){referenceHeight=height;StableSeconds=0;}
            StableSeconds+=Mathf.Min(delta,.1f);
            if(StableSeconds<.75f)return false;
            Pending=false;Calibrated=true;confirmed=false;return true;
        }
    }
}
