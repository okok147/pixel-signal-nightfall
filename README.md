# Pixel Signal: Nightfall — Unity 2D

一個沒有第三方素材依賴的原創 survivor-like 2D pixel-art game。場景使用專案內的原創 `NightfallArena.png`，角色、敵群、投射物、經驗碎片、寶箱與 HUD 在執行時生成，因此專案很輕，適合先在 Unity 裡快速迭代。

## 開啟

1. 將此 repository clone 後，使用 Unity Hub 開啟 repository 根目錄。
2. 目前本機已安裝並對齊 Unity 6.5 `6000.5.6f1`。
3. 開啟 `Assets/Scenes/Main.unity`，按 Play。

第一次匯入時，`Assets/Editor/SignalDriftSceneSetup.cs` 會自動建立 Main Camera 與場景。

## 操作

- `Enter` / `Space`：開始
- `WASD` / 方向鍵：移動
- `Space`：脈衝，清除附近敵人（遊戲開始後）
- `P`：暫停／繼續
- `R`：勝利或失敗後重新開始
- `F`：切換全螢幕

目標是在 180 秒內建立自己的自動攻擊 loadout 並活過敵群。擊敗敵人會掉落 XP signal shards；升級時會暫停並提供 3 張卡；`Spark Wand + Ember Ring + Wand Lv.3` 可進化成 `Cinder Volley`。專案目前採用程式生成的 pixel-art 素材，沒有下載第三方素材。

場景背景使用原創的生成式像素材質，角色、敵人、投射物與互動單位則使用程式生成的清晰像素輪廓，並以深色地面陰影整合到場景。遊戲設計與 UI/UX 對照請看 [`GAME_DESIGN.md`](GAME_DESIGN.md)。這是同類型玩法的原創實作，不複製其他遊戲的角色、素材、商標名稱或精確版面。
