using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 도면(진) 전체 목록 + 자모/글자 조회. 임포터가 채운다.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Spell Diagram Library", fileName = "SpellDiagramLibrary")]
    public sealed class SpellDiagramLibrarySO : SerializedScriptableObject
    {
        public List<SpellDiagramSO> Diagrams = new List<SpellDiagramSO>();

        public SpellDiagramSO FindByLetter(string letter)
        {
            foreach (var d in Diagrams)
                if (d != null && d.Letter == letter) return d;
            return null;
        }

        public SpellDiagramSO FindByJamo(string initial, string medial, string final)
        {
            foreach (var d in Diagrams)
                if (d != null && d.Initial == initial && d.Medial == medial && d.Final == final) return d;
            return null;
        }
    }
}
