using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Game : Node2D {
    [Export] public Level[] levels;
    [Export] public float[] levelXPositions;
    [Export] public Vector2[] levelZooms;
    [Export] public int currentLevelIndex = 0;
    [Export] private SceneTransition transition;
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

    private Stack<TurnSnapshot> turnStack = new Stack<TurnSnapshot>();
    private TurnSnapshot currentTurn;
    private bool isTransitioning = false;
    private bool undoGrace = false;
    public bool CanUndo => !this.isTransitioning && this.turnStack.Count > 0;
    public bool CanStartTurn => !this.isTransitioning;
    public bool IsUndoGraceActive => this.undoGrace;

    public override async void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);
        if (this.transition == null) {
            GD.PrintErr("⚠ this.transition 未设置，请在 Inspector 绑定 SceneTransition 节点！");
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
        this.turnStack.Clear();
        this.currentTurn = null;
        this.isTransitioning = false;
        await this.transition.FadeFromBlack(transitionTime);
    }

    public async Task SwitchLevel(int newIndex) {
        if (newIndex < 0 || newIndex >= levelXPositions.Length) return;
        if (newIndex == currentLevelIndex) return;
        this.isTransitioning = true;
        await this.transition.FadeToBlack(transitionTime);
        this.currentLevelIndex = newIndex;
        this.camera.Offset = new Vector2(levelXPositions[currentLevelIndex], camera.Offset.Y);
        this.camera.Zoom = levelZooms[currentLevelIndex];
        this.player.tileLayer = this.levels[currentLevelIndex].tileMapLayer;
        Vector2 markerGlobalPos = this.levels[currentLevelIndex].marker2D.GlobalPosition;
        this.player.GlobalPosition = new Vector2(markerGlobalPos.X, markerGlobalPos.Y);
        this.player.ClearHistory();
        this.turnStack.Clear();
        this.currentTurn = null;
        await this.transition.FadeFromBlack(transitionTime);
        this.isTransitioning = false;
    }

    public async Task PlayFinalCameraAnimation() {
        this.isTransitioning = true;
        this.turnStack.Clear();
        this.currentTurn = null;
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
        this.currentTurn = new TurnSnapshot {
            playerPrevCell = playerCurrentCell,
            playerPrevCombat = player.isCombatPower
        };
        Level level = levels[currentLevelIndex];
        Node zombiesNode = level.GetNodeOrNull<Node>("Zombies");
        if (zombiesNode != null) {
            foreach (Node child in zombiesNode.GetChildren()) {
                if (child is Zombie z && z.tileLayer != null) {
                    Vector2I zCell = z.tileLayer.LocalToMap(z.Position);
                    this.currentTurn.zombieSnapshots.Add(new ZombieSnapshot
                        { path = z.GetPath(), cell = zCell, wasDead = !z.Visible });
                }
            }
        }
    }

    public void RegisterBlood(Node2D blood) {
        if (this.currentTurn != null) this.currentTurn.bloodsAdded.Add(blood);
    }

    public void RegisterFoodPicked(string scenePath, Vector2 foodGlobalPosition) {
        if (this.currentTurn != null)
            this.currentTurn.foodsPicked.Add(new FoodPicked { scenePath = scenePath, globalPos = foodGlobalPosition });
    }

    public void EndTurn() {
        if (this.currentTurn != null) {
            this.turnStack.Push(this.currentTurn);
            this.currentTurn = null;
        }
    }

    public async Task UndoLastTurnAsync() {
        if (this.turnStack.Count == 0) return;
        TurnSnapshot snap = this.turnStack.Pop();
        foreach (Node2D blood in snap.bloodsAdded) {
            if (IsInstanceValid(blood)) blood.QueueFree();
        }
        player.isCombatPower = snap.playerPrevCombat;
        this.undoGrace = true;
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
        this.undoGrace = false;
    }
}