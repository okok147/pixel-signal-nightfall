# Pixel Signal: Nightfall — Survivor-like vertical slice

## Design boundary

這是一個原創的 survivor-like 2D pixel game。它借鑑同一類型的可理解機制：角色移動但武器自動攻擊、敵群隨時間增壓、經驗拾取、升級選擇、武器／被動道具組合、進化、寶箱、計時生存與結算；不使用原作角色、名稱、素材、音效、文字或一比一版面。

## Core loop

1. 進入夜間場景，玩家只控制移動。
2. `Spark Wand` 自動瞄準最近敵人並發射。
3. 敵人從場景外緣進場，追逐玩家；擊敗後留下 `signal shard` 經驗碎片。
4. 玩家靠近碎片自動吸取；達到 XP 門檻時，遊戲暫停並出現 3 張升級卡。
5. 被動道具改變拾取半徑、移動速度、生命值或加入環繞攻擊。
6. `Spark Wand + Ember Ring + Wand Lv.3` 可選擇進化成 `Cinder Volley`。
7. 隨生存時間增加，敵人生成間隔縮短、敵人種類與生命增強；定期出現寶箱，開啟後給予大量 XP 與分數。
8. 撐過 180 秒勝利；生命歸零則失敗。

## UI / UX contract

- 頂部固定 HUD：生存時間、等級、XP bar、生命、分數、擊殺數、寶箱數。
- 開始畫面：一句目標、清楚的控制提示、Enter 開始。
- 升級畫面：時間與敵人暫停；三張大卡片可用滑鼠或數字鍵 1/2/3 選擇；每張卡有類型、名稱、效果與可讀的單句說明。
- 暫停畫面：保留當前 run 的 HUD，上層只顯示 Resume 提示。
- 寶箱回饋：畫面底部短暫顯示獎勵 toast。
- 結算畫面：存活時間、等級、擊殺與 XP，R 重新開始。
- 文字與像素素材使用自有命名與自有色板，讓玩家理解系統但不誤認為原作介面。
- 視覺採混合方向：背景保留較有材質與深度的深夜 relay field，角色和互動單位保持有限色板、清晰像素輪廓，並以低透明度地面陰影統一光照關係。

## First-pass content

- Weapon: Spark Wand → Cinder Volley evolution
- Passive: Ember Ring, Magnet, Vitality, Haste
- Enemy: Red Drone、Brute Drone
- Reward: signal shard XP、periodic chest
- Controls: WASD / arrows move, Space pulse, P pause, R restart, F fullscreen
