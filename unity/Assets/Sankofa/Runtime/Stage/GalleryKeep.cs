using Sankofa.Interaction;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using UnityEngine;
using TMPro;
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
        [SerializeField] TMP_Text hint;
        [SerializeField] Renderer rim;
        MaterialPropertyBlock feedback;
        PalmReceiveGesture[] receive;
        readonly DepositGesture deposit=new DepositGesture();
        StageObject item;HandSource carrying;
        public Transform Recess => recess;
        bool replacementConfirmed;
        void Awake(){feedback=new MaterialPropertyBlock();receive=new PalmReceiveGesture[hands.Length];for(var i=0;i<hands.Length;i++)receive[i]=new PalmReceiveGesture();}
        void ResetGesture(){replacementConfirmed=false;carrying=null;deposit.Reset();foreach(var g in receive)g.Reset();}
        void Update()
        {
            var flow=app.Flow;
            var available=flow!=null&&flow.Phase==VisitPhase.Closing&&flow.CanKeep&&flow.CurrentLine==null&&!StageClock.Paused;
            if(hint!=null)
            {
                hint.gameObject.SetActive(available);
                var replacing=flow.KeptVisit!=null;
                hint.text=carrying==null
                    ? replacing?"Receive to replace\nyour previous kept visit":"Receive the keepsake\nto keep this visit"
                    : deposit.Armed
                        ? replacing&&!replacementConfirmed?"Close to confirm replacement.\nYour previous visit will be removed."
                            : replacing?"Open, then close again\nto replace your previous visit":"Close your hand\nto keep this visit"
                        : replacing&&replacementConfirmed?"Open over the recess;\nclose again to replace":"Hold an open palm\nover the gold recess";
            }
            if(rim!=null){rim.GetPropertyBlock(feedback);feedback.SetFloat("_Highlight",available?deposit.Progress:0);rim.SetPropertyBlock(feedback);}
            if(!available)
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
            // A replacement needs a second deliberate open/dwell/close after the disclosure.
            // Until then the old record is untouched, and leaving/pause cancels this confirmation.
            if(flow.KeptVisit!=null&&!replacementConfirmed)
            { replacementConfirmed=true;deposit.Reset();return; }
            if(app.KeepVisit())
            {
                item.MoveTo(recess.position,0.5f);
                ResetGesture();
            }
            else deposit.Reset(); // Stay in the hand after failure; an explicit open/dwell/release can retry.
        }
    }
}
