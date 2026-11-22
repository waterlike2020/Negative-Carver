namespace NegativeCarver.Core.Tests;

public sealed class PilotComponentTests
{
    [Fact]
    public void Empty_components_reference_each_other_without_pilot_numbers()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs"));
        var cutter = File.ReadAllText(Path.Combine(scripts, "CutterController.cs"));
        var tile = File.ReadAllText(Path.Combine(scripts, "BlockTile.cs"));
        var fx = File.ReadAllText(Path.Combine(scripts, "CarveFxManager.cs"));
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Pilot.unity"));

        Assert.Contains("CutterController cutter", grid, StringComparison.Ordinal);
        Assert.Contains("CarveFxManager carveFx", grid, StringComparison.Ordinal);
        Assert.Contains("GridManager grid", cutter, StringComparison.Ordinal);
        Assert.Contains("CarveFxManager carveFx", cutter, StringComparison.Ordinal);
        Assert.Contains("GridManager grid", tile, StringComparison.Ordinal);
        Assert.Contains("GridManager grid", fx, StringComparison.Ordinal);

        Assert.Contains("guid: a11c0e3d5b7f49e8a2d64c9f0b1e8a73", scene, StringComparison.Ordinal);
        Assert.Contains("guid: b22d1f4e6c8049f9b3e75d0a1c2f9b84", scene, StringComparison.Ordinal);
        Assert.Contains("guid: d44f31608ea251b1d5097f2c3e41bd06", scene, StringComparison.Ordinal);

        foreach (var source in new[] { grid, cutter, tile, fx })
        {
            var normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal);
            Assert.Contains("namespace NegativeCarver.Unity\n{", normalized, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace NegativeCarver.Unity;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("= 10", source, StringComparison.Ordinal);
            Assert.DoesNotContain("= 18", source, StringComparison.Ordinal);
            Assert.DoesNotContain("= 15", source, StringComparison.Ordinal);
            Assert.DoesNotContain("= 30", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Hard_drop_lands_then_calls_carve_with_cell_offsets_and_origin()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var cutter = File.ReadAllText(Path.Combine(scripts, "CutterController.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        var hardDrop = MethodBody(cutter, "public void HardDrop()");
        var update = MethodBody(cutter, "private void Update()");
        var advance = MethodBody(cutter, "private void AdvanceFall()");
        var finish = MethodBody(cutter, "private void FinishDrop(CutterTouch touch)");
        var begin = MethodBody(cutter, "private void BeginFall()");
        Assert.Contains("fallElapsed = PilotRules.FallSecondsPerCell;", begin, StringComparison.Ordinal);
        Assert.Contains("BeginFall();", hardDrop, StringComparison.Ordinal);
        var alreadyFalling = hardDrop.IndexOf("if (falling)", StringComparison.Ordinal);
        var arm = hardDrop.IndexOf("BeginFall();", StringComparison.Ordinal);
        Assert.True(alreadyFalling >= 0);
        Assert.True(alreadyFalling < arm);
        Assert.Contains("return;", BodyOf(hardDrop, "if (falling)"));
        Assert.DoesNotContain("fallElapsed", BodyOf(hardDrop, "if (falling)"), StringComparison.Ordinal);
        Assert.DoesNotContain("DropToFirstContact", hardDrop, StringComparison.Ordinal);
        Assert.DoesNotContain("CarveArea", hardDrop, StringComparison.Ordinal);
        Assert.DoesNotContain("0.5", hardDrop, StringComparison.Ordinal);
        Assert.True(update.IndexOf("commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);", StringComparison.Ordinal)
            < update.IndexOf("AdvanceFall();", StringComparison.Ordinal));
        Assert.Contains("fallElapsed < PilotRules.FallSecondsPerCell", advance, StringComparison.Ordinal);
        Assert.Contains("fallElapsed = 0f;", advance, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(advance, "grid.Descend(piece);"));
        Assert.DoesNotContain("while", advance, StringComparison.Ordinal);
        var descend = advance.IndexOf("grid.Descend(piece);", StringComparison.Ordinal);
        var finishCall = advance.IndexOf("FinishDrop(descended);", StringComparison.Ordinal);
        Assert.True(descend >= 0);
        Assert.True(finishCall > descend);
        var carve = finish.IndexOf("grid.CarveArea(pattern, new Vector2Int(0, 0));", StringComparison.Ordinal);
        var collapse = finish.IndexOf("LastChainCount = grid.CheckAndCollapseLines();", StringComparison.Ordinal);
        var spawn = finish.IndexOf("SpawnNext();", StringComparison.Ordinal);
        Assert.True(carve >= 0);
        Assert.True(collapse > carve);
        Assert.True(spawn > collapse);
        Assert.Contains("new Vector2Int(cells[i].X, cells[i].Y)", finish, StringComparison.Ordinal);
        Assert.Contains("Steer(grid.Sidestep(piece, -1));", MethodBody(cutter, "public void MoveLeft()"), StringComparison.Ordinal);
        Assert.Contains("Steer(grid.Sidestep(piece, 1));", MethodBody(cutter, "public void MoveRight()"), StringComparison.Ordinal);
        var steer = MethodBody(cutter, "private void Steer(CutterTouch touch)");
        var locked = BodyOf(steer, "if (touch.Locked)");
        Assert.Contains("FinishDrop(touch);", locked, StringComparison.Ordinal);
        Assert.DoesNotContain("FinishDrop", BodyOf(steer, "if (touch.Moved)"), StringComparison.Ordinal);

        var forward = MethodBody(grid, "public void DropToFirstContact(CutterPiece piece)");
        Assert.Contains("piece.DropToFirstContact(board);", forward, StringComparison.Ordinal);

        var carveBody = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        Assert.Contains("board.CarveSolids(origin.x, origin.y, pattern)", carveBody, StringComparison.Ordinal);
        Assert.Contains("for (var i = 0; i < carved.Count; i++)", carveBody, StringComparison.Ordinal);
        Assert.Contains("Destroy(tile.gameObject)", carveBody, StringComparison.Ordinal);
        Assert.DoesNotContain("TryClear", carveBody, StringComparison.Ordinal);
        var effect = carveBody.IndexOf("carveFx.PlayCarveEffect(new Vector3(center.X, center.Y, 0f));", StringComparison.Ordinal);
        var destroy = carveBody.IndexOf("Destroy(tile.gameObject)", StringComparison.Ordinal);
        Assert.True(effect >= 0);
        Assert.True(destroy > effect);
    }

    [Fact]
    public void Each_carved_cell_plays_one_burst_that_is_not_tied_to_the_tile()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var fx = File.ReadAllText(Path.Combine(scripts, "CarveFxManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        var carveBody = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var carved = carveBody.IndexOf("var carved = board.CarveSolids(origin.x, origin.y, pattern);", StringComparison.Ordinal);
        var loop = carveBody.IndexOf("for (var i = 0; i < carved.Count; i++)", StringComparison.Ordinal);
        var center = carveBody.IndexOf("var center = BoardSpace.CellCenter(cell.X, cell.Y);", StringComparison.Ordinal);
        var effect = carveBody.IndexOf("carveFx.PlayCarveEffect(new Vector3(center.X, center.Y, 0f));", StringComparison.Ordinal);
        var destroy = carveBody.IndexOf("Destroy(tile.gameObject)", StringComparison.Ordinal);
        Assert.True(carved >= 0);
        Assert.True(loop > carved);
        Assert.True(center > loop);
        Assert.True(effect > center);
        Assert.True(destroy > effect);
        Assert.Equal(1, CountOf(carveBody, "carveFx.PlayCarveEffect("));
        Assert.DoesNotContain("StartCoroutine", carveBody, StringComparison.Ordinal);
        Assert.DoesNotContain("tile.transform", carveBody, StringComparison.Ordinal);

        var play = MethodBody(fx, "public void PlayCarveEffect(Vector3 worldPos)");
        var spawn = play.IndexOf("var burst = new GameObject(\"CarveBurst\");", StringComparison.Ordinal);
        var detach = play.IndexOf("burst.transform.SetParent(null);", StringComparison.Ordinal);
        var place = play.IndexOf("burst.transform.position = worldPos;", StringComparison.Ordinal);
        var stop = play.IndexOf("particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);", StringComparison.Ordinal);
        var duration = play.IndexOf("main.duration = 0.35f;", StringComparison.Ordinal);
        var once = play.IndexOf("particles.Play();", StringComparison.Ordinal);
        Assert.True(spawn >= 0);
        Assert.True(detach > spawn);
        Assert.True(place > detach);
        Assert.True(stop > place);
        Assert.True(duration > stop);
        Assert.True(once > duration);
        Assert.Contains("catch (System.Exception exception)", play, StringComparison.Ordinal);
        Assert.Contains("Debug.LogException(exception);", play, StringComparison.Ordinal);
        Assert.Contains("main.loop = false;", play, StringComparison.Ordinal);
        Assert.Contains("emission.rateOverTime = 0f;", play, StringComparison.Ordinal);
        Assert.Contains("new ParticleSystem.Burst(0f, 12)", play, StringComparison.Ordinal);
        Assert.Contains("ParticleSystemStopAction.Destroy", play, StringComparison.Ordinal);
        Assert.DoesNotContain("tile", play, StringComparison.Ordinal);
        Assert.DoesNotContain("StartCoroutine", play, StringComparison.Ordinal);
        Assert.DoesNotContain("yield", play, StringComparison.Ordinal);
        Assert.DoesNotContain("UnityEngine", File.ReadAllText(Path.Combine(root, "src", "NegativeCarver.Core", "Board.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Overlapping_shakes_return_the_camera_to_the_original_frame()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var fx = File.ReadAllText(Path.Combine(scripts, "CarveFxManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        var carveBody = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var cellLoop = BodyOf(carveBody, "for (var i = 0; i < carved.Count; i++)");
        var destroy = carveBody.IndexOf("Destroy(tile.gameObject)", StringComparison.Ordinal);
        var shake = carveBody.IndexOf("carveFx.PlayCameraShake();", StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCameraShake", cellLoop, StringComparison.Ordinal);
        Assert.True(destroy >= 0);
        Assert.True(shake > destroy);
        Assert.Equal(1, CountOf(carveBody, "carveFx.PlayCameraShake();"));
        var gate = BodyOf(carveBody, "if (carveFx != null && (carved.Count > 0 || softened > 0))");
        Assert.Contains("carveFx.PlayCameraShake();", gate, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(gate, "carveFx.PlayCameraShake();"));

        var enable = MethodBody(fx, "private void OnEnable()");
        Assert.Contains("RememberFrame();", enable, StringComparison.Ordinal);
        var disable = MethodBody(fx, "private void OnDisable()");
        Assert.Contains("RestoreFrame();", disable, StringComparison.Ordinal);

        var play = MethodBody(fx, "public void PlayCameraShake()");
        var guard = BodyOf(play, "if (framedCamera == null)");
        Assert.Contains("RememberFrame();", guard, StringComparison.Ordinal);
        Assert.DoesNotContain("shakeSerial", guard, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(play, "RememberFrame();"));
        var remember = play.IndexOf("RememberFrame();", StringComparison.Ordinal);
        var serial = play.IndexOf("shakeSerial++;", StringComparison.Ordinal);
        var stop = play.IndexOf("StopCoroutine(shakeRoutine);", StringComparison.Ordinal);
        var start = play.IndexOf("shakeRoutine = StartCoroutine(Shake(shakeSerial));", StringComparison.Ordinal);
        Assert.True(remember >= 0);
        Assert.True(serial > remember);
        Assert.True(stop > serial);
        Assert.True(start > stop);
        Assert.DoesNotContain("framedPosition = framedCamera.transform.position", play, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", play, StringComparison.Ordinal);
        var caught = BodyOf(play, "catch (System.Exception exception)");
        Assert.Contains("Debug.LogException(exception);", caught, StringComparison.Ordinal);
        Assert.Contains("RestoreFrame();", caught, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", caught, StringComparison.Ordinal);

        var motion = MethodBody(fx, "private IEnumerator Shake(int serial)");
        Assert.Contains("var duration = 0.16f;", motion, StringComparison.Ordinal);
        var wave = BodyOf(motion, "while (elapsed < duration)");
        Assert.Contains("framedCamera.transform.position = framedPosition + new Vector3(offset.x, offset.y, 0f);", wave, StringComparison.Ordinal);
        Assert.DoesNotContain("RestoreFrame", wave, StringComparison.Ordinal);
        Assert.DoesNotContain("RememberFrame", wave, StringComparison.Ordinal);
        var waveAt = motion.IndexOf("while (elapsed < duration)", StringComparison.Ordinal);
        var restore = motion.IndexOf("RestoreFrame();", waveAt, StringComparison.Ordinal);
        Assert.True(waveAt >= 0);
        Assert.True(restore > waveAt);
        Assert.DoesNotContain("framedPosition =", motion, StringComparison.Ordinal);

        var memory = MethodBody(fx, "private void RememberFrame()");
        Assert.Contains("framedCamera = Camera.main;", memory, StringComparison.Ordinal);
        Assert.Contains("framedPosition = framedCamera.transform.position;", memory, StringComparison.Ordinal);
        var back = MethodBody(fx, "private void RestoreFrame()");
        Assert.Contains("framedCamera.transform.position = framedPosition;", back, StringComparison.Ordinal);
    }

    [Fact]
    public void Carve_and_collapse_sounds_use_different_clips_and_stay_silent_without_one()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var fx = File.ReadAllText(Path.Combine(scripts, "CarveFxManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        var carveBody = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var gate = BodyOf(carveBody, "if (carveFx != null && (carved.Count > 0 || softened > 0))");
        Assert.Contains("carveFx.PlayCarveSound(impact);", gate, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(gate, "carveFx.PlayCarveSound(impact);"));
        Assert.Equal(1, CountOf(grid, "carveFx.PlayCarveSound(impact);"));
        Assert.DoesNotContain("PlayCollapseSound", carveBody, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCarveSound", MethodBody(grid, "public int CheckAndCollapseLines()"), StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCollapseSound", MethodBody(grid, "public int CheckAndCollapseLines()"), StringComparison.Ordinal);

        var carveSound = MethodBody(fx, "public void PlayCarveSound(CarveImpact impact)");
        var collapseSound = MethodBody(fx, "public void PlayCollapseSound()");
        var small = carveSound.IndexOf("impact == CarveImpact.Small", StringComparison.Ordinal);
        var playSmall = carveSound.IndexOf("PlayClip(smallClip);", StringComparison.Ordinal);
        var leave = carveSound.IndexOf("return;", playSmall, StringComparison.Ordinal);
        var big = carveSound.IndexOf("impact == CarveImpact.Big", StringComparison.Ordinal);
        var playBig = carveSound.IndexOf("PlayClip(bigClip);", StringComparison.Ordinal);
        Assert.True(small >= 0);
        Assert.True(playSmall > small);
        Assert.True(leave > playSmall);
        Assert.True(big > leave);
        Assert.True(playBig > big);
        Assert.Equal(2, CountOf(carveSound, "PlayClip("));
        Assert.DoesNotContain("collapseClip", carveSound, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCollapseSound", carveSound, StringComparison.Ordinal);
        Assert.Contains("PlayClip(collapseClip);", collapseSound, StringComparison.Ordinal);
        Assert.DoesNotContain("smallClip", collapseSound, StringComparison.Ordinal);
        Assert.DoesNotContain("bigClip", collapseSound, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCarveSound", collapseSound, StringComparison.Ordinal);

        var clip = MethodBody(fx, "private void PlayClip(AudioClip clip)");
        var guard = BodyOf(clip, "if (clip == null || voice == null)");
        Assert.Contains("return;", guard, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayOneShot", guard, StringComparison.Ordinal);
        var played = clip.IndexOf("voice.PlayOneShot(clip);", StringComparison.Ordinal);
        var opened = clip.IndexOf("if (clip == null || voice == null)", StringComparison.Ordinal);
        Assert.True(opened >= 0);
        Assert.True(played > opened);
        Assert.Contains("catch (System.Exception exception)", clip, StringComparison.Ordinal);
        Assert.Contains("Debug.LogException(exception);", clip, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", clip, StringComparison.Ordinal);

        var collapse = MethodBody(fx, "public void PlayCollapseEffect(int lineY)");
        Assert.Contains("PlayCollapseSound();", collapse, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCarveSound", collapse, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayOneShot", collapse, StringComparison.Ordinal);
    }

    [Fact]
    public void Play_shows_a_cutter_preview_and_replaces_it_after_the_carve()
    {
        var root = FindRepositoryRoot();
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Pilot.unity"));

        var start = MethodBody(cutter, "private void Start()");
        var finish = MethodBody(cutter, "private void FinishDrop(CutterTouch touch)");
        Assert.Contains("SpawnNext();", start, StringComparison.Ordinal);
        Assert.Contains("SpawnNext();", finish, StringComparison.Ordinal);

        var spawnNext = MethodBody(cutter, "private void SpawnNext()");
        var clearCall = spawnNext.IndexOf("ClearPreview();", StringComparison.Ordinal);
        var deal = spawnNext.IndexOf("piece = queue.Next();", StringComparison.Ordinal);
        var showCall = spawnNext.IndexOf("ShowPreview();", StringComparison.Ordinal);
        Assert.True(clearCall >= 0);
        Assert.True(deal > clearCall);
        Assert.True(showCall > deal);

        var show = MethodBody(cutter, "private void ShowPreview()");
        var originX = show.IndexOf("piece.OriginX + cells[i].X", StringComparison.Ordinal);
        var originY = show.IndexOf("piece.OriginY + cells[i].Y", StringComparison.Ordinal);
        var center = show.IndexOf("BoardSpace.CellCenter(x, y)", StringComparison.Ordinal);
        var place = show.IndexOf("new Vector3(center.X, center.Y, 0f)", StringComparison.Ordinal);
        Assert.True(originX >= 0);
        Assert.True(originY > originX);
        Assert.True(center > originY);
        Assert.True(place > center);
        Assert.Contains("BlockType.CutterPreview", show, StringComparison.Ordinal);
        Assert.Contains("previewTilePrefab", show, StringComparison.Ordinal);

        var clear = MethodBody(cutter, "private void ClearPreview()");
        Assert.Contains("Destroy(previewTiles[i].gameObject)", clear, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoveTile", clear, StringComparison.Ordinal);
        Assert.DoesNotContain("TryClear", clear, StringComparison.Ordinal);
        Assert.DoesNotContain(".Remove()", clear, StringComparison.Ordinal);

        Assert.Contains("guid: d6f518c90ab482ddf4273c1609e5d861", scene, StringComparison.Ordinal);
        Assert.Contains("previewTilePrefab:", scene, StringComparison.Ordinal);
    }

    [Fact]
    public void Keyboard_runs_each_command_once_per_frame()
    {
        var root = FindRepositoryRoot();
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var update = MethodBody(cutter, "private void Update()");

        Assert.Contains("Keyboard.current", update, StringComparison.Ordinal);
        Assert.Contains("wasPressedThisFrame", update, StringComparison.Ordinal);
        Assert.DoesNotContain("isPressed", update, StringComparison.Ordinal);
        Assert.DoesNotContain("GetKeyDown", update, StringComparison.Ordinal);
        Assert.DoesNotContain("GetKey(", update, StringComparison.Ordinal);

        Assert.Equal("commands.Add(CutterCommand.Left);", BodyOf(update, "if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)"));
        Assert.Equal("commands.Add(CutterCommand.Right);", BodyOf(update, "if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)"));
        Assert.Equal("commands.Add(CutterCommand.Rotate);", BodyOf(update, "if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)"));
        Assert.Equal("commands.Add(CutterCommand.HardDrop);", BodyOf(update, "if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)"));
        AssertOnce(update, "commands.ConsumeInOrder(MoveLeft, MoveRight, Rotate, HardDrop);");
        Assert.DoesNotContain("TryConsume", update, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckPenetration_reads_the_board_and_does_not_change_it()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var body = MethodBody(grid, "public bool CheckPenetration(int y)");

        Assert.Equal("if (board == null)\n            {\n                return false;\n            }\n\n            return board.CheckPenetration(y);", body.Trim().Trim('{', '}').Trim());
        Assert.DoesNotContain("Destroy", body, StringComparison.Ordinal);
        Assert.DoesNotContain("TryClear", body, StringComparison.Ordinal);
        Assert.DoesNotContain("TrySet", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyGravity", body, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCollapseEffect", body, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyGravity_moves_tiles_with_the_board_in_the_same_call()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var body = MethodBody(grid, "public void ApplyGravity()");
        var loop = BodyOf(body, "for (var i = 0; i < rows.Length; i++)");
        var shift = loop.IndexOf("board.ShiftRowDown(row);", StringComparison.Ordinal);
        var tiles = loop.IndexOf("ShiftTilesDown(row);", StringComparison.Ordinal);
        var tileBody = MethodBody(grid, "private void ShiftTilesDown(int row)");
        var copy = tileBody.IndexOf("tiles[x, y] = tiles[x, y + 1];", StringComparison.Ordinal);
        var place = tileBody.IndexOf("var center = BoardSpace.CellCenter(x, y);", StringComparison.Ordinal);
        var move = tileBody.IndexOf("tile.transform.position = new Vector3(center.X, center.Y, 0f);", StringComparison.Ordinal);
        var bind = tileBody.IndexOf("tile.Bind(this, x, y, board.Get(x, y));", StringComparison.Ordinal);
        var clearTop = tileBody.IndexOf("tiles[x, board.Height - 1] = null;", StringComparison.Ordinal);

        Assert.Contains("board.PenetratedRowsHighToLow()", body, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(body, "for (var i = 0; i < rows.Length; i++)"));
        Assert.True(shift >= 0);
        Assert.True(tiles > shift);
        Assert.True(copy >= 0);
        Assert.True(place > copy);
        Assert.True(move > place);
        Assert.True(bind > move);
        Assert.True(clearTop > bind);
        Assert.DoesNotContain("Destroy", body, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCollapseEffect", body, StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StartCoroutine", body, StringComparison.Ordinal);
    }

    [Fact]
    public void One_drop_repeats_collapse_and_keeps_the_chain_count()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var body = MethodBody(grid, "public int CheckAndCollapseLines()");
        var data = body.IndexOf("var rows = board.CollapseChains();", StringComparison.Ordinal);
        var tiles = body.IndexOf("ShiftTilesDown(rows[i]);", StringComparison.Ordinal);
        var record = body.IndexOf("Match.RecordChain(rows.Length);", StringComparison.Ordinal);
        var count = body.IndexOf("return rows.Length;", StringComparison.Ordinal);

        Assert.Contains("public MatchState Match { get; } = new MatchState();", grid, StringComparison.Ordinal);
        Assert.True(data >= 0);
        Assert.True(tiles > data);
        Assert.True(record > tiles);
        Assert.True(count > record);
        Assert.Equal(1, CountOf(body, "Match.RecordChain(rows.Length);"));
        Assert.DoesNotContain("StartCoroutine", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Each_collapsed_row_plays_one_effect_after_the_chain_is_recorded()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var fx = File.ReadAllText(Path.Combine(scripts, "CarveFxManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var body = MethodBody(grid, "public int CheckAndCollapseLines()");
        var record = body.IndexOf("Match.RecordChain(rows.Length);", StringComparison.Ordinal);
        var call = body.IndexOf("carveFx.PlayCollapseEffect(rows[i]);", StringComparison.Ordinal);
        var done = body.IndexOf("return rows.Length;", StringComparison.Ordinal);
        Assert.True(record >= 0);
        Assert.True(call > record);
        Assert.True(done > call);
        Assert.Equal(1, CountOf(body, "carveFx.PlayCollapseEffect(rows[i]);"));
        Assert.Equal(1, CountOf(grid, "carveFx.PlayCollapseEffect("));

        var gate = BodyOf(body, "if (carveFx != null)");
        var wave = BodyOf(gate, "for (var i = 0; i < rows.Length; i++)");
        Assert.Equal("carveFx.PlayCollapseEffect(rows[i]);", wave);
        Assert.DoesNotContain("PlayCollapseEffect", BodyOf(body, "for (var i = 0; i < rows.Length; i++)"), StringComparison.Ordinal);
        Assert.DoesNotContain("PlayCollapseEffect", MethodBody(grid, "public void ApplyGravity()"), StringComparison.Ordinal);

        var effect = MethodBody(fx, "public void PlayCollapseEffect(int lineY)");
        Assert.Contains("PlayCollapseSound();", effect, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(effect, "PlayCollapseSound();"));
        Assert.Contains("catch (System.Exception exception)", effect, StringComparison.Ordinal);
        Assert.Contains("Debug.LogException(exception);", effect, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", effect, StringComparison.Ordinal);
    }

    [Fact]
    public void Removing_the_effect_component_leaves_carve_collapse_and_the_verdict()
    {
        var root = FindRepositoryRoot();
        var scripts = Path.Combine(root, "Assets", "Scripts");
        var cutter = File.ReadAllText(Path.Combine(scripts, "CutterController.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(scripts, "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.DoesNotContain("carveFx", MethodBody(cutter, "public void MoveLeft()"), StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", MethodBody(cutter, "public void MoveRight()"), StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", MethodBody(cutter, "public void Rotate()"), StringComparison.Ordinal);
        var hardDrop = MethodBody(cutter, "private void FinishDrop(CutterTouch touch)");
        Assert.DoesNotContain("carveFx", hardDrop, StringComparison.Ordinal);
        var carveCall = hardDrop.IndexOf("grid.CarveArea(pattern, new Vector2Int(0, 0));", StringComparison.Ordinal);
        var collapseCall = hardDrop.IndexOf("LastChainCount = grid.CheckAndCollapseLines();", StringComparison.Ordinal);
        var verdict = hardDrop.IndexOf("grid.Match.RecordDrop(grid.HasSolidRockOnDefeatLine());", StringComparison.Ordinal);
        var cleared = hardDrop.IndexOf("IsCleared()", verdict, StringComparison.Ordinal);
        Assert.True(carveCall >= 0);
        Assert.True(collapseCall > carveCall);
        Assert.True(verdict > collapseCall);
        Assert.True(cleared > verdict);
        Assert.DoesNotContain("carveFx", MethodBody(cutter, "private void Update()"), StringComparison.Ordinal);

        var carve = MethodBody(grid, "public void CarveArea(System.Collections.Generic.List<Vector2Int> cutterPattern, Vector2Int origin)");
        var solids = carve.IndexOf("board.CarveSolids(origin.x, origin.y, pattern);", StringComparison.Ordinal);
        var carveGate = carve.IndexOf("if (carveFx != null)", StringComparison.Ordinal);
        var burst = BodyOf(carve, "if (carveFx != null)");
        Assert.True(solids >= 0);
        Assert.True(carveGate > solids);
        Assert.Contains("carveFx.PlayCarveEffect(", burst, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(carve, "carveFx.PlayCarveEffect("));
        Assert.Equal(1, CountOf(burst, "carveFx.PlayCarveEffect("));
        Assert.DoesNotContain("carveFx == null", carve, StringComparison.Ordinal);
        Assert.DoesNotContain("return;", burst, StringComparison.Ordinal);

        var collapse = MethodBody(grid, "public int CheckAndCollapseLines()");
        var chains = collapse.IndexOf("var rows = board.CollapseChains();", StringComparison.Ordinal);
        var record = collapse.IndexOf("Match.RecordChain(rows.Length);", StringComparison.Ordinal);
        var effect = collapse.IndexOf("if (carveFx != null)", StringComparison.Ordinal);
        var result = collapse.IndexOf("return rows.Length;", StringComparison.Ordinal);
        Assert.True(chains >= 0);
        Assert.True(record > chains);
        Assert.True(effect > record);
        Assert.True(result > effect);
        Assert.DoesNotContain("carveFx == null", collapse, StringComparison.Ordinal);
        Assert.DoesNotContain("return;", BodyOf(collapse, "if (carveFx != null)"), StringComparison.Ordinal);

        Assert.DoesNotContain("carveFx", MethodBody(grid, "public bool CheckCoreGemFreed()"), StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", MethodBody(grid, "public bool HasSolidRockOnDefeatLine()"), StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", File.ReadAllText(Path.Combine(root, "src", "NegativeCarver.Core", "MatchState.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Gravity_logs_an_error_only_when_the_core_overlap_flag_is_set()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var apply = MethodBody(grid, "public void ApplyGravity()");
        var collapse = MethodBody(grid, "public int CheckAndCollapseLines()");
        var warn = MethodBody(grid, "private void WarnIfCoreOverlapped()");

        var clear = apply.IndexOf("board.ClearCoreOverlap();", StringComparison.Ordinal);
        var shift = apply.IndexOf("board.ShiftRowDown(row);", StringComparison.Ordinal);
        var applyWarn = apply.IndexOf("WarnIfCoreOverlapped();", StringComparison.Ordinal);
        var chains = collapse.IndexOf("var rows = board.CollapseChains();", StringComparison.Ordinal);
        var collapseWarn = collapse.IndexOf("WarnIfCoreOverlapped();", StringComparison.Ordinal);
        var guarded = BodyOf(warn, "if (board != null && board.CoreOverlapDetected)");

        Assert.True(clear >= 0);
        Assert.True(shift > clear);
        Assert.True(applyWarn > shift);
        Assert.Equal(1, CountOf(apply, "board.ClearCoreOverlap();"));
        Assert.Equal(1, CountOf(apply, "WarnIfCoreOverlapped();"));
        Assert.True(chains >= 0);
        Assert.True(collapseWarn > chains);
        Assert.Equal(1, CountOf(collapse, "WarnIfCoreOverlapped();"));
        Assert.DoesNotContain("ClearCoreOverlap", collapse, StringComparison.Ordinal);
        Assert.Contains("Debug.LogError(\"Core gem overlaps another block.\");", guarded, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(warn, "Debug.LogError(\"Core gem overlaps another block.\");"));
        Assert.Equal(1, CountOf(grid, "Debug.LogError(\"Core gem overlaps another block.\");"));
        Assert.DoesNotContain("PlayCollapseEffect", warn, StringComparison.Ordinal);
        Assert.DoesNotContain("Destroy", warn, StringComparison.Ordinal);
    }

    private static string BodyOf(string source, string header)
    {
        var start = source.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var open = source.IndexOf('{', start);
        Assert.True(open >= 0);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(open + 1, i - open - 1).Trim();
                }
            }
        }

        throw new InvalidOperationException("Block was not closed: " + header);
    }

    private static int CountOf(string source, string text)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var found = source.IndexOf(text, start, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count++;
            start = found + text.Length;
        }
    }

    private static void AssertOnce(string source, string text)
    {
        var first = source.IndexOf(text, StringComparison.Ordinal);
        Assert.True(first >= 0);
        Assert.Equal(-1, source.IndexOf(text, first + text.Length, StringComparison.Ordinal));
    }

    private static string MethodBody(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var open = source.IndexOf('{', start);
        Assert.True(open >= 0);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(open, i - open + 1);
                }
            }
        }

        throw new InvalidOperationException("Method body was not closed: " + signature);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("NegativeCarver.sln was not found above the test output directory.");
    }
}
