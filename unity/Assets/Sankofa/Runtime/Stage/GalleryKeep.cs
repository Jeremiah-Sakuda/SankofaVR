using Sankofa.Interaction;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using UnityEngine;
namespace Sankofa.Runtime.Stage
{
    /// <summary>Receive the closing memento, carry it over the labelled recess, then deliberately release to keep.</summary>
    [DefaultExecutionOrder(160)]
    public sealed class GalleryKeep : MonoBehaviour
    {
        [SerializeField] SankofaApp app;
        [SerializeField] StoryStage stage;
        [SerializeField] HandSource[] hands;
        [SerializeField] Transform recess;
        PalmReceiveGesture[] receive;
        readonly DepositGesture deposit=new DepositGesture();
        StageObject item;HandSource carrying;
        public Transform Recess => recess;
        void Awake(){receive=new PalmReceiveGesture[hands.Length];for(var i=0;i<hands.Length;i++)receive[i]=new PalmReceiveGesture();}
        void ResetGesture(){carrying=null;deposit.Reset();foreach(var g in receive)g.Reset();}
        void Update()
        {
            var flow=app.Flow;
            if(flow==null||flow.Phase!=VisitPhase.Closing||!flow.CanKeep||flow.CurrentLine!=null||StageClock.Paused)
            { ResetGesture();item=null;return; }
            var current=stage.Find(flow.Plan.Plan.Keepsake);
            if(current==null||!current.Visible)return;
            if(item!=current){ResetGesture();item=current;}
            if(carrying==null)
            {
                var at=item.transform.position;var target=new System.Numerics.Vector3(at.x,at.y,at.z);
                for(var i=0;i<hands.Length;i++)
                {
                    if(hands[i].Holding){receive[i].Reset();continue;}
                    if(receive[i].Update(hands[i].Sample,target,Time.deltaTime))
                    { carrying=hands[i];stage.Carry(item,carrying);break; }
                }
                return;
            }
            var tracked=carrying.Tracked&&!carrying.Holding;
            var open=tracked&&HandGeometry.Openness(carrying.Sample)>=1.3f;
            var near=tracked&&Vector3.Distance(carrying.PalmCenter,recess.position)<0.16f;
            if(!deposit.Update(tracked,near,open,Time.deltaTime))return;
            item.MoveTo(recess.position,0.5f);
            app.KeepVisit();
            ResetGesture();
        }
    }
}
