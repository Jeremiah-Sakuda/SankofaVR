using System;
using System.Collections.Generic;
using Sankofa.Core.Story;
using Sankofa.Runtime.Stage;
using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>Explicit question chips in the direction of available story objects. Flow supplies spoiler-safe questions.</summary>
    [DefaultExecutionOrder(120)]
    public sealed class ObjectQuestions : MonoBehaviour
    {
        [SerializeField] StoryStage stage;
        [SerializeField] Transform head;
        [SerializeField] PokePanel[] panels;
        [SerializeField] PokePanel fallbackPanel;
        readonly List<(string,string)> fallback=new List<(string,string)>();
        readonly Dictionary<string,int> subjectRows=new Dictionary<string,int>();
        IReadOnlyList<Question> available = Array.Empty<Question>();
        public PokePanel[] Panels => panels;
        public event Action<string> Pressed;
        public bool Hidden { get; set; }
        void Awake() { foreach(var p in panels) p.Pressed += id => Pressed?.Invoke(id); }
        public void Show(IReadOnlyList<Question> questions) => available = questions;
        public void Pulse(string id) { foreach(var panel in panels)panel.Pulse(id); }
        void Update()
        {
            var index=0;fallback.Clear();subjectRows.Clear();
            foreach(var q in available)
            {
                var target=q.About==null?null:stage.Find(q.About);
                if(target == null || !target.Visible || target.Scene!=stage.ActiveScene || index>=panels.Length)
                {fallback.Add(("q:"+q.Id,q.Label));continue;}
                var p=panels[index++];
                var direction=target.AttentionCentre-head.position;
                subjectRows.TryGetValue(q.About,out var row);subjectRows[q.About]=row+1;
                // Keep the chip comfortable and within the pointer's reach, in the direction of its actual subject.
                var at=head.position+direction.normalized*Mathf.Min(direction.magnitude,0.85f)+Vector3.up*(0.08f+row*0.10f);
                p.transform.position=at;
                p.transform.rotation=Quaternion.LookRotation(at-head.position,Vector3.up);
                p.Show(new[]{("q:"+q.Id,q.Label)}); p.Hidden=Hidden || StageClock.Paused;
            }
            for(;index<panels.Length;index++) panels[index].Hidden=true;
            if(fallbackPanel!=null){fallbackPanel.Show(fallback);fallbackPanel.Hidden=Hidden||StageClock.Paused;}
        }
    }
}
