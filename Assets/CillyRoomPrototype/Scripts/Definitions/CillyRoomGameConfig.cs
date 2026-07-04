using System.Collections.Generic;
using UnityEngine;

namespace CillyRoomPrototype
{
    [CreateAssetMenu(menuName = "CillyRoom/Game Config", fileName = "CillyRoomGameConfig")]
    public sealed class CillyRoomGameConfig : ScriptableObject
    {
        public CillyRoomArtSet artSet;
        public CillyRoomTextConfig textConfig;
        public bool useFixedSeed;
        public int randomSeed = 2026;
        public List<CillyRoomSymbolDefinition> startingSymbols = new List<CillyRoomSymbolDefinition>();
        public List<CillyRoomLevelDefinition> levels = new List<CillyRoomLevelDefinition>();
    }
}
