using System.Collections.Generic;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>Moves a caption band away from the offered/carried object without moving the story or accepting input.
    /// Placement holds for a line and focus set, so small hand movements cannot make the text chase the hand.</summary>
    [DefaultExecutionOrder(650)]
    public sealed class GalleryCaptionPlacement : MonoBehaviour
    {
        [SerializeField] CaptionBoard board;
        [SerializeField] StoryStage stage;
        [SerializeField] SankofaApp app;
        [SerializeField] Camera eye;
        [SerializeField] StageObject[] alwaysProtect = new StageObject[0];
        [SerializeField] StageObject[] protectWhenRevealed = new StageObject[0];
        [SerializeField] PokePanel[] protectedPanels = new PokePanel[0];
        readonly List<Rect> protectedRects=new List<Rect>();
        readonly List<Renderer> panelSurfaces=new List<Renderer>();
        static readonly Vector2[] candidates={Vector2.zero,new Vector2(-0.24f,0),new Vector2(-0.40f,0),new Vector2(-0.50f,0),new Vector2(0,-0.18f),new Vector2(0,-0.34f),new Vector2(0,-0.46f),new Vector2(0,0.22f),new Vector2(0,0.40f),new Vector2(-0.40f,0.22f),new Vector2(-0.50f,0.22f),new Vector2(-0.40f,0.40f),new Vector2(-0.50f,0.40f),new Vector2(0.40f,0.22f),new Vector2(0.40f,0.40f)};
        string lastLine;int lastFocus;
        public Rect CaptionRect => Project(eye,board.CaptionText.GetComponent<Renderer>().bounds);
        void LateUpdate() => LayoutNow();
        public void LayoutNow()
        {
            if(board.Caption.Length==0){board.PlaceContent(Vector2.zero);lastLine=null;return;}
            protectedRects.Clear();var signature=17;
            var invited=app.Flow?.OpenInvitation?.Object;
            var line=app.Flow?.CurrentLine;
            foreach(var item in stage.Objects)
            {
                if(!item.gameObject.activeInHierarchy)continue;
                var named=false;
                if(line!=null&&System.Array.IndexOf(protectWhenRevealed,item)>=0)
                    foreach(var action in line.Actions)if(action.Object==item.Id)named=true;
                if(!item.IsCarried&&item.Id!=invited&&System.Array.IndexOf(alwaysProtect,item)<0&&!named)continue;
                var drawn=false;foreach(var surface in item.Surfaces)if(surface.enabled)drawn=true;
                if(!drawn)continue;
                signature=unchecked(signature*31+item.GetInstanceID());
                foreach(var surface in item.Surfaces)
                    if(surface.enabled){var rect=Project(eye,surface.bounds);if(rect.width>0)protectedRects.Add(Padded(rect,0.02f));}
            }
            foreach(var panel in protectedPanels)
            {
                if(panel==null||panel.Hidden)continue;
                signature=unchecked(signature*31+panel.Revision);
                panelSurfaces.Clear();panel.GetComponentsInChildren(false,panelSurfaces);
                foreach(var surface in panelSurfaces)
                    if(surface.enabled&&surface.gameObject.activeInHierarchy)
                    {var rect=Project(eye,surface.bounds);if(rect.width>0)protectedRects.Add(Padded(rect,0.008f));}
            }
            signature=unchecked(signature*31+eye.fieldOfView.GetHashCode()+eye.aspect.GetHashCode()+(int)board.Size);
            if(lastLine==board.Caption&&lastFocus==signature)
            {
                var clear=true;foreach(var rect in protectedRects)if(Overlap(CaptionRect,rect)>0.00005f)clear=false;
                if(clear)return;
            }
            lastLine=board.Caption;lastFocus=signature;
            var best=Vector2.zero;var score=float.PositiveInfinity;
            foreach(var candidate in candidates)
            {
                board.PlaceContent(candidate);board.CaptionText.ForceMeshUpdate();
                var rect=CaptionRect;var centre=eye.transform.InverseTransformPoint(board.CaptionText.GetComponent<Renderer>().bounds.center);
                if(rect.xMin<0.035f||rect.xMax>0.965f||rect.yMin<0.04f||rect.yMax>0.93f||Mathf.Abs(Mathf.Atan2(centre.y,centre.z)*Mathf.Rad2Deg)>28f)continue;
                var overlap=0f;foreach(var other in protectedRects)overlap+=Overlap(rect,other);
                var cost=overlap*1000+candidate.sqrMagnitude;
                if(cost<score){score=cost;best=candidate;}
                if(overlap==0)break;
            }
            board.PlaceContent(best);
        }
        public static float Overlap(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
        static Rect Padded(Rect rect,float pad)=>Rect.MinMaxRect(rect.xMin-pad,rect.yMin-pad,rect.xMax+pad,rect.yMax+pad);
        public static Rect Project(Camera camera,Bounds bounds)
        {
            if(camera.transform.InverseTransformPoint(bounds.center).z<=0)return default;
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(var i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var projected=camera.WorldToViewportPoint(corner);if(projected.z<=0)return default;
                min=Vector2.Min(min,projected);max=Vector2.Max(max,projected);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
    }
}
