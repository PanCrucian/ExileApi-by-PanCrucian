using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class TheGame : RemoteMemoryObject
    {
        private static readonly (string Name, int Offset)[] ModernStateLayout =
        {
            ("AreaLoadingState", 0x48),
            ("WaitingState", 0x58),
            ("CreditsState", 0x68),
            ("EscapeState", 0x78),
            ("InGameState", 0x88),
            ("ChangePasswordState", 0x98),
            ("LoginState", 0xA8),
            ("PreGameState", 0xB8),
            ("CreateCharacterState", 0xC8),
            ("SelectCharacterState", 0xD8),
            ("DeleteCharacterState", 0xE8),
            ("LoadingState", 0xF8)
        };

        private static readonly string[] RequiredStateNames =
        {
            "PreGameState",
            "LoginState",
            "SelectCharacterState",
            "WaitingState",
            "InGameState",
            "LoadingState",
            "EscapeState",
            "AreaLoadingState"
        };

        //I hope this caching will works fine
        private static long PreGameStatePtr = -1;
        private static long LoginStatePtr = -1;
        private static long SelectCharacterStatePtr = -1;
        private static long WaitingStatePtr = -1;
        private static long InGameStatePtr = -1;
        private static long LoadingStatePtr = -1;
        private static long EscapeStatePtr = -1;
        private static TheGame Instance;
        private readonly CachedValue<int> _AreaChangeCount;
        private readonly CachedValue<bool> _inGame;
        public readonly Dictionary<string, GameState> AllGameStates;
        private readonly int CurrentAreaHashOff;
        private readonly int DataOff;

        public TheGame(IMemory m, Cache cache)
        {
            pM = m;
            pCache = cache;
            pTheGame = this;
            Instance = this;
            Address = m.Read<long>(m.BaseOffsets[OffsetsName.GameStateOffset] + m.AddressOfProcess);
            _AreaChangeCount = new TimeCache<int>(() => M.Read<int>(M.AddressOfProcess + M.BaseOffsets[OffsetsName.AreaChangeCount]), 50);

            AllGameStates = BuildGameStates();
            LogMissingGameStates(AllGameStates);

            PreGameStatePtr = GetStateAddress("PreGameState");
            LoginStatePtr = GetStateAddress("LoginState");
            SelectCharacterStatePtr = GetStateAddress("SelectCharacterState");
            WaitingStatePtr = GetStateAddress("WaitingState");
            InGameStatePtr = GetStateAddress("InGameState");
            LoadingStatePtr = GetStateAddress("LoadingState");
            EscapeStatePtr = GetStateAddress("EscapeState");
            LoadingState = GetStateAs<AreaLoadingState>("AreaLoadingState");
            IngameState = GetStateAs<IngameState>("InGameState");

            Core.Logger.Information(
                $"TheGame modern bootstrap -> AreaLoadingState=0x{LoadingState.Address:X}, InGameState=0x{IngameState.Address:X}, " +
                $"LoadingState=0x{LoadingStatePtr:X}, LoginState=0x{LoginStatePtr:X}, PreGameState=0x{PreGameStatePtr:X}");

            _inGame = new FrameCache<bool>(
                () => IngameState.Address != 0 && IngameState.Data.Address != 0 && IngameState.ServerData.Address != 0 && !IsLoading /*&&
                                                 IngameState.ServerData.IsInGame*/);

            Files = new FilesContainer(m);
            DataOff = Extensions.GetOffset<IngameStateOffsets>(nameof(IngameStateOffsets.Data));
            CurrentAreaHashOff = Extensions.GetOffset<IngameDataOffsets>(nameof(IngameDataOffsets.CurrentAreaHash));
        }

        public FilesContainer Files { get; set; }
        public AreaLoadingState LoadingState { get; }
        public IngameState IngameState { get; }
        public IList<GameState> CurrentGameStates => M.ReadDoublePtrVectorClasses<GameState>(Address + 0x8, this, true);
        public IList<GameState> ActiveGameStates => M.ReadDoublePtrVectorClasses<GameState>(Address + 0x20, this, true);
        public bool IsPreGame => GameStateActive(PreGameStatePtr);
        public bool IsLoginState => GameStateActive(LoginStatePtr);
        public bool IsSelectCharacterState => GameStateActive(SelectCharacterStatePtr);
        public bool IsWaitingState => GameStateActive(WaitingStatePtr); //This happens after selecting character, maybe other cases
        public bool IsInGameState => GameStateActive(InGameStatePtr); //In game, with selected character
        public bool IsLoadingState => GameStateActive(LoadingStatePtr);
        public bool IsEscapeState => GameStateActive(EscapeStatePtr);
        public bool IsLoading => GameStateActive(LoadingState.Address) || GameStateActive(LoadingStatePtr);
        public int AreaChangeCount => _AreaChangeCount.Value;
        public bool InGame => _inGame.Value;

        public uint CurrentAreaHash
        {
            get
            {
                if (IngameState.Address == 0)
                    return 0;

                var dataAddress = M.Read<long>(IngameState.Address + DataOff);
                if (dataAddress == 0)
                    return 0;

                var hash = M.Read<uint>(IngameState.Address + DataOff, CurrentAreaHashOff);
                return hash;
            }
        }

        public void Init()
        {
        }

        private static bool GameStateActive(long stateAddress)
        {
            if (stateAddress <= 0)
                return false;

            var gameStateController = Instance;
            if (gameStateController == null) return false;
            var M = gameStateController.M;
            return VectorContains(M, Instance.Address + 0x20, stateAddress) || VectorContains(M, Instance.Address + 0x8, stateAddress);
        }

        private Dictionary<string, GameState> BuildGameStates()
        {
            var result = new Dictionary<string, GameState>(StringComparer.Ordinal);

            MergeStates(result, BuildModernGameStates());

            if (!result.ContainsKey("InGameState") || !result.ContainsKey("AreaLoadingState"))
            {
                Core.Logger.Warning("TheGame: modern fixed-slot parse was incomplete, falling back to legacy discovery paths.");
                MergeStates(result, TryReadHashMap(Address + 0x48, "hashmap@+0x48"));
                MergeStates(result, TryReadStateVector(Address + 0x8, "vector@+0x8"));
                MergeStates(result, TryReadStateVector(Address + 0x20, "vector@+0x20"));
            }

            Core.Logger.Information(
                $"TheGame: combined states ({result.Count}) -> {string.Join(", ", result.Keys)}");

            return result;
        }

        private Dictionary<string, GameState> BuildModernGameStates()
        {
            var result = new Dictionary<string, GameState>(StringComparer.Ordinal);

            foreach (var state in ModernStateLayout)
            {
                var stateAddress = M.Read<long>(Address + state.Offset);
                if (stateAddress == 0)
                    continue;

                result[state.Name] = GetObject<GameState>(stateAddress);
            }

            Core.Logger.Information(
                $"TheGame: modern fixed-slot parse yielded {result.Count} states -> {string.Join(", ", result.Keys)}");

            return result;
        }

        private static void MergeStates(Dictionary<string, GameState> target, Dictionary<string, GameState> source)
        {
            foreach (var kv in source)
            {
                if (!target.ContainsKey(kv.Key))
                    target[kv.Key] = kv.Value;
            }
        }

        private Dictionary<string, GameState> TryReadHashMap(long pointer, string source)
        {
            try
            {
                var states = ReadHashMap(pointer);
                Core.Logger.Information(
                    $"TheGame: {source} yielded {states.Count} states -> {string.Join(", ", states.Keys)}");
                return states;
            }
            catch (Exception e)
            {
                Core.Logger.Error($"TheGame: {source} failed -> {e}");
                return new Dictionary<string, GameState>(StringComparer.Ordinal);
            }
        }

        private Dictionary<string, GameState> TryReadStateVector(long pointer, string source)
        {
            var result = new Dictionary<string, GameState>(StringComparer.Ordinal);

            try
            {
                var start = M.Read<long>(pointer);
                var last = M.Read<long>(pointer + 0x10);
                var length = (int) (last - start);

                if (start == 0 || last == 0 || length <= 0 || length > 4096)
                {
                    Core.Logger.Information(
                        $"TheGame: {source} skipped (start=0x{start:X}, last=0x{last:X}, length={length}).");
                    return result;
                }

                var bytes = M.ReadMem(start, length);

                for (var readOffset = 0; readOffset + 8 <= length; readOffset += 16)
                {
                    var stateAddress = BitConverter.ToInt64(bytes, readOffset);
                    if (stateAddress == 0)
                        continue;

                    var state = GetObject<GameState>(stateAddress);
                    var stateName = state.StateName;

                    if (string.IsNullOrWhiteSpace(stateName))
                        continue;

                    result[stateName] = state;
                }

                Core.Logger.Information(
                    $"TheGame: {source} yielded {result.Count} states -> {string.Join(", ", result.Keys)}");
            }
            catch (Exception e)
            {
                Core.Logger.Error($"TheGame: {source} failed -> {e}");
            }

            return result;
        }

        private Dictionary<string, GameState> ReadHashMap(long pointer)
        {
            var result = new Dictionary<string, GameState>();

            var stack = new Stack<GameStateHashNode>();
            var visited = new HashSet<long>();
            var startNode = ReadObject<GameStateHashNode>(pointer);
            var item = startNode.Root;
            stack.Push(item);
            var guard = 0;

            while (stack.Count != 0)
            {
                var node = stack.Pop();
                if (node == null || node.Address == 0)
                    continue;

                if (!visited.Add(node.Address))
                    continue;

                if (++guard > 2048)
                {
                    throw new InvalidOperationException("GameState hash map traversal guard triggered. Offsets or structure may be outdated.");
                }

                if (!node.IsNull)
                    result[node.Key] = node.Value1;

                var prev = node.Previous;

                if (!prev.IsNull)
                    stack.Push(prev);

                var next = node.Next;

                if (!next.IsNull)
                    stack.Push(next);
            }

            return result;
        }

        private long GetStateAddress(string stateName)
        {
            return AllGameStates.TryGetValue(stateName, out var state) ? state.Address : 0;
        }

        private T GetStateAs<T>(string stateName) where T : RemoteMemoryObject, new()
        {
            return AllGameStates.TryGetValue(stateName, out var state) ? state.AsObject<T>() : GetObject<T>(0);
        }

        internal string TryResolveKnownStateName(long stateAddress)
        {
            foreach (var state in AllGameStates)
            {
                if (state.Value.Address == stateAddress)
                    return state.Key;
            }

            return null;
        }

        private static bool VectorContains(IMemory memory, long vectorAddress, long stateAddress)
        {
            var start = memory.Read<long>(vectorAddress);
            var last = memory.Read<long>(vectorAddress + 0x10);
            var length = (int) (last - start);

            if (start == 0 || last == 0 || length <= 0 || length > 4096)
                return false;

            var bytes = memory.ReadMem(start, length);

            for (var readOffset = 0; readOffset < length; readOffset += 16)
            {
                var pointer = BitConverter.ToInt64(bytes, readOffset);
                if (stateAddress == pointer)
                    return true;
            }

            return false;
        }

        private static void LogMissingGameStates(Dictionary<string, GameState> gameStates)
        {
            var missingStates = new List<string>();

            foreach (var state in RequiredStateNames)
            {
                if (!gameStates.ContainsKey(state))
                    missingStates.Add(state);
            }

            if (missingStates.Count > 0)
            {
                Core.Logger.Warning(
                    $"TheGame: missing non-critical states after bootstrap: {string.Join(", ", missingStates)}. Parsed states: {string.Join(", ", gameStates.Keys)}");
            }
        }

        private class GameStateHashNode : RemoteMemoryObject
        {
            public GameStateHashNode Previous => ReadObject<GameStateHashNode>(Address);
            public GameStateHashNode Root => ReadObject<GameStateHashNode>(Address + 0x8);
            public GameStateHashNode Next => ReadObject<GameStateHashNode>(Address + 0x10);

            //public readonly byte Unknown;
            public bool IsNull => M.Read<byte>(Address + 0x19) != 0;

            //private readonly byte byte_0;
            //private readonly byte byte_1;
            public string Key => M.ReadNativeString(Address + 0x20);

            //public readonly int Useless;
            public GameState Value1 => ReadObject<GameState>(Address + 0x40);

            //public readonly long Value2;
        }
    }

    public class GameState : RemoteMemoryObject
    {
        private string stateName;
        public string StateName => stateName ?? (stateName = TheGame?.TryResolveKnownStateName(Address) ?? M.ReadNativeString(Address + 0x10));

        public override string ToString()
        {
            return StateName;
        }
    }

    public class AreaLoadingState : GameState
    {
        //This is actualy pointer to loading screen stuff (image, etc), but should works fine.
        public bool IsLoading => TheGame != null && TheGame.IsLoading;
        public string AreaName => M.ReadStringU(M.Read<long>(Address + 0x1F0));

        public override string ToString()
        {
            return $"{AreaName}, IsLoading: {IsLoading}";
        }
    }
}
