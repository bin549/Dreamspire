using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Game : Node2D {
    [Export] public Level[] levels;
    [Export] public float[] levelXPositions;
    [Export] public Vector2[] levelZooms;
    [Export] public int currentLevelIndex = 0;
    [Export] private SceneTransition _transition;
    [Export] private float transitionTime = 0.5f;
    [Export] private Camera2D camera;
    [Export] private Player player;

    private class ZombieSnapshot {
        public NodePath path;
        public Vector2I cell;
        public bool wasDead;
    }

    private class TurnSnapshot {
        public Vector2I playerPrevCell;
        public bool playerPrevCombat;
        public List<ZombieSnapshot> zombieSnapshots = new List<ZombieSnapshot>();
        public List<Node2D> bloodsAdded = new List<Node2D>();
        public List<FoodPicked> foodsPicked = new List<FoodPicked>();
    }

    private struct FoodPicked {
        public string scenePath;
        public Vector2 globalPos;
    }

    private Stack<TurnSnapshot> _turnStack = new Stack<TurnSnapshot>();
    private TurnSnapshot _currentTurn;
    private bool _isTransitioning = false;
    private bool _undoGrace = false;

    public bool CanUndo => !_isTransitioning && _turnStack.Count > 0;
    public bool CanStartTurn => !_isTransitioning;
    public bool IsUndoGraceActive => _undoGrace;

    public override async void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);
        if (_transition == null) {
            GD.PrintErr("⚠ _transition 未设置，请在 Inspector 绑定 SceneTransition 节点！");
            return;
        }
        if (levelXPositions == null || levelXPositions.Length == 0) {
            GD.PrintErr("⚠ levelXPositions 未设置，请在 Inspector 填写每个关卡的X坐标！");
            return;
        }
        if (levelZooms == null || levelZooms.Length != levelXPositions.Length) {
            GD.PrintErr("⚠ levelZooms 未设置或数量与 levelXPositions 不一致！");
            return;
        }
        if (camera == null) {
            GD.PrintErr("⚠ camera 未设置，请在 Inspector 绑定 Camera2D 节点！");
            return;
        }
        camera.Offset = new Vector2(levelXPositions[currentLevelIndex], camera.Offset.Y);
        this.player.tileLayer = this.levels[currentLevelIndex].tileMapLayer;
        Vector2 markerGlobalPos = this.levels[currentLevelIndex].marker2D.GlobalPosition;
        this.player.GlobalPosition = new Vector2(markerGlobalPos.X, markerGlobalPos.Y);
        this.player.ClearHistory();
        _turnStack.Clear();
        _currentTurn = null;
        _isTransitioning = false;
        await _transition.FadeFromBlack(transitionTime);
    }

    public async Task SwitchLevel(int newIndex) {
        if (newIndex < 0 || newIndex >= levelXPositions.Length) return;
        if (newIndex == currentLevelIndex) return;
        _isTransitioning = true;
        await _transition.FadeToBlack(transitionTime);
        this.currentLevelIndex = newIndex;
        this.camera.Offset = new Vector2(levelXPositions[currentLevelIndex], camera.Offset.Y);
        this.camera.Zoom = levelZooms[currentLevelIndex];
        this.player.tileLayer = this.levels[currentLevelIndex].tileMapLayer;
        Vector2 markerGlobalPos = this.levels[currentLevelIndex].marker2D.GlobalPosition;
        this.player.GlobalPosition = new Vector2(markerGlobalPos.X, markerGlobalPos.Y);
        this.player.ClearHistory();
        _turnStack.Clear();
        _currentTurn = null;
        await _transition.FadeFromBlack(transitionTime);
        _isTransitioning = false;
    }

    public async Task PlayFinalCameraAnimation() {
        _isTransitioning = true;
        _turnStack.Clear();
        _currentTurn = null;
        float minX = levelXPositions[0];
        float maxX = levelXPositions[levelXPositions.Length - 1];
        float centerX = (minX + maxX) * 0.5f;
        Vector2 startOffset = camera.Offset;
        Vector2 endOffset = new Vector2(centerX - 64, startOffset.Y);
        Vector2 startZoom = camera.Zoom;
        Vector2 endZoom = startZoom * 0.3f;
        float duration = 1.2f;
        float t = 0f;
        while (t < duration) {
            t += (float)GetProcessDeltaTime();
            float alpha = Mathf.Clamp(t / duration, 0f, 1f);
            float eased = Mathf.Ease(alpha, 0.8f);
            camera.Offset = startOffset.Lerp(endOffset, eased);
            camera.Zoom = startZoom.Lerp(endZoom, eased);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        camera.Offset = endOffset;
        camera.Zoom = endZoom;
    }

    public void BeginTurn(Vector2I playerCurrentCell) {
        _currentTurn = new TurnSnapshot {
            playerPrevCell = playerCurrentCell,
            playerPrevCombat = player.isCombatPower
        };
        Level level = levels[currentLevelIndex];
        Node zombiesNode = level.GetNodeOrNull<Node>("Zombies");
        if (zombiesNode != null) {
            foreach (Node child in zombiesNode.GetChildren()) {
                if (child is Zombie z && z.tileLayer != null) {
                    Vector2I zCell = z.tileLayer.LocalToMap(z.Position);
                    _currentTurn.zombieSnapshots.Add(new ZombieSnapshot { path = z.GetPath(), cell = zCell, wasDead = !z.Visible });
                }
            }
        }
    }

    public void RegisterBlood(Node2D blood) {
        if (_currentTurn != null) _currentTurn.bloodsAdded.Add(blood);
    }

    public void RegisterFoodPicked(string scenePath, Vector2 foodGlobalPosition) {
        if (_currentTurn != null) _currentTurn.foodsPicked.Add(new FoodPicked { scenePath = scenePath, globalPos = foodGlobalPosition });
    }

    public void EndTurn() {
        if (_currentTurn != null) {
            _turnStack.Push(_currentTurn);
            _currentTurn = null;
        }
    }

    public async Task UndoLastTurnAsync() {
        if (_turnStack.Count == 0) return;
        TurnSnapshot snap = _turnStack.Pop();
        foreach (Node2D blood in snap.bloodsAdded) {
            if (IsInstanceValid(blood)) blood.QueueFree();
        }
        player.isCombatPower = snap.playerPrevCombat;
        _undoGrace = true;
        if (player.IsDead) player.Revive();
        foreach (ZombieSnapshot zsnap in snap.zombieSnapshots) {
            Zombie z = GetNodeOrNull<Zombie>(zsnap.path);
            if (z != null) {
                if (!zsnap.wasDead) z.Revive();
                z.ForceReposition(zsnap.cell);
            }
        }
        player.SetToCellWithoutEvents(snap.playerPrevCell);
        if (snap.foodsPicked.Count > 0) {
            float wait = Mathf.Max(player.MoveTime, 0.08f) + 0.01f;
            await ToSignal(GetTree().CreateTimer(wait), SceneTreeTimer.SignalName.Timeout);
            Level level = levels[currentLevelIndex];
            Node foodsNode = level.GetNodeOrNull<Node>("Foods");
            if (foodsNode != null) {
                foreach (var fp in snap.foodsPicked) {
                    PackedScene foodScene = GD.Load<PackedScene>(fp.scenePath);
                    if (foodScene == null) continue;
                    Node inst = foodScene.Instantiate();
                    foodsNode.AddChild(inst);
                    if (inst is Node2D n2) n2.GlobalPosition = fp.globalPos;
                }
            }
        }
        float grace = Mathf.Max(player.MoveTime, 0.08f) + 0.02f;
        await ToSignal(GetTree().CreateTimer(grace), SceneTreeTimer.SignalName.Timeout);
        _undoGrace = false;
    }
}
