namespace Sankofa.Interaction
{
    /// <summary>An explicit deposit: an open held hand dwells at the destination, then closes while still there.
    /// Loss of tracking, a pause, or leaving the destination cancels arming; reappearing closed cannot deposit.</summary>
    public sealed class DepositGesture
    {
        double held;
        bool armed, done;
        public bool Armed => armed;
        public void Reset() { held=0;armed=done=false; }
        public bool Update(bool tracked,bool inPlace,bool open,double seconds)
        {
            if(done)return false;
            if(!tracked||!inPlace){held=0;armed=false;return false;}
            if(open){held+=System.Math.Max(0,seconds);armed=held>=0.45;return false;}
            if(!armed){held=0;return false;}
            done=true;return true;
        }
    }
}
