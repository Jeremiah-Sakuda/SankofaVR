using System;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    [Serializable]
    public sealed class IllustrationResponse
    {
        public string Object;
        public IllustratedFigure[] Figures;
        public int AcceptedPose = 2;
        public int PassivePose;
        public int Layer;
    }

    /// <summary>Layered scenery unfolds around the seated visitor. Responses come only from resolved invitations.</summary>
    public sealed class IllustratedTheatre : MonoBehaviour
    {
        [SerializeField] Renderer[] layers;
        [SerializeField] IllustrationResponse[] responses;
        [SerializeField] StoryTransition transition;
        [Tooltip("Where what the visitor sets down for the painted people comes to rest: on the painted ground between them.")]
        [SerializeField] Transform tradeGround;
        [SerializeField] float settleSeconds = 2.2f;
        [SerializeField] float crossSeconds = 2.6f;
        MaterialPropertyBlock block;
        StageObject placed, placedOn;
        float placedFor;
        float elapsed;
        int selectedLayer = -1;
        float selection;
        public int AcceptedResponses { get; private set; }
        public float UnfoldTime => elapsed;
        public int SelectedLayer => selectedLayer;

        void Awake() => block = new MaterialPropertyBlock();
        void OnEnable() => ResetState();

        public void ResetState()
        {
            elapsed = 0;
            selectedLayer = -1;
            selection = 0;
            AcceptedResponses = 0;
            placed = placedOn = null;
            CrossedIntoPainting = false;
            Apply();
        }

        /// <summary>
        /// Something set down within the visitor's reach for the painted people (the flour on the hide) settles, then
        /// both cross into the painting together and rest between them: the exchange happens where they stand.
        /// </summary>
        public void Placed(StageObject item, StageObject on)
        {
            if (tradeGround == null || item == null || on == null) return;
            placed = item;
            placedOn = on;
            placedFor = 0;
        }

        /// <summary>True once something set down has been sent across into the painting.</summary>
        public bool CrossedIntoPainting { get; private set; }

        public void Resolve(StageObject item, bool accepted)
        {
            if (item == null || responses == null) return;
            foreach (var response in responses)
            {
                if (response.Object != item.Id) continue;
                foreach (var figure in response.Figures)
                    if (figure != null) figure.SetPose(accepted ? response.AcceptedPose : response.PassivePose);
                if (accepted)
                {
                    AcceptedResponses++;
                    selectedLayer = response.Layer;
                    selection = 0;
                }
                break;
            }
        }

        void Update()
        {
            elapsed += StageClock.DeltaTime;
            if (placed != null && (placedFor += StageClock.DeltaTime) >= settleSeconds)
            {
                var onTop = placedOn.PlacePosition - placedOn.transform.position;
                placedOn.MoveTo(tradeGround.position, crossSeconds);
                placed.MoveTo(tradeGround.position + onTop, crossSeconds);
                CrossedIntoPainting = true;
                placed = placedOn = null;
            }
            selection = Mathf.MoveTowards(selection, 1, StageClock.DeltaTime / 0.8f);
            Apply();
        }

        void Apply()
        {
            if (layers == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                layers[i].GetPropertyBlock(block);
                // Reduced motion reveals the composition together; the regular view draws far-to-near.
                var unfold = transition != null && transition.ReducedMotion ? 1 : Mathf.Clamp01((elapsed - i * 0.35f) / 1.5f);
                block.SetFloat("_Unfold", unfold);
                block.SetFloat("_Selection", i == selectedLayer ? selection : 0);
                layers[i].SetPropertyBlock(block);
            }
        }
    }
}
