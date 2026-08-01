(() => {
  "use strict";

  const canvas = document.getElementById("game");
  const ctx = canvas.getContext("2d");
  const W = canvas.width;
  const H = canvas.height;
  const PLAY = { left: 28, top: 88, right: W - 28, bottom: H - 28 };
  const TARGET = 8;
  const ROUND_SECONDS = 45;
  const TAU = Math.PI * 2;
  const UI_FONT = '"Trebuchet MS", "Avenir Next", system-ui, sans-serif';
  const DISPLAY_FONT = 'Georgia, "Times New Roman", serif';

  const COLORS = {
    nightInk: "#21192B",
    nightViolet: "#17152B",
    meadow: "#273B36",
    meadowDeep: "#1D302C",
    meadowLight: "#3D5A48",
    parchment: "#F6E8BD",
    cream: "#FFF2C9",
    mint: "#8EE3C2",
    lavender: "#BFA7FF",
    coral: "#FF758F",
    peach: "#FFB98B",
    gold: "#FFD56A",
    timber: "#6F4938",
    timberDark: "#4B302D",
    inkSoft: "#493649",
    white: "#FFF9E7",
    muted: "#C7B992",
  };

  const keys = new Set();
  const state = {
    mode: "menu",
    elapsed: 0,
    score: 0,
    collected: 0,
    health: 3,
    pulseCooldown: 0,
    pulseEnergy: 100,
    invulnerable: 0,
    shake: 0,
    flash: 0,
    rng: 19,
    player: { x: W / 2, y: H / 2 + 78, vx: 0, vy: 0, r: 14, angle: 0 },
    signal: null,
    hazards: [],
    particles: [],
    pulses: [],
    meadow: [],
    flowers: [],
    stones: [],
    fireflies: [],
  };

  function rand() {
    state.rng = (state.rng * 1664525 + 1013904223) >>> 0;
    return state.rng / 4294967296;
  }

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function distance(a, b) {
    return Math.hypot(a.x - b.x, a.y - b.y);
  }

  function formatTime(seconds) {
    const remaining = Math.max(0, Math.ceil(ROUND_SECONDS - seconds));
    return `00:${String(remaining).padStart(2, "0")}`;
  }

  function clippedPath(x, y, width, height, cut = 8) {
    const c = Math.min(cut, width / 3, height / 3);
    ctx.beginPath();
    ctx.moveTo(x + c, y);
    ctx.lineTo(x + width - c, y);
    ctx.lineTo(x + width, y + c);
    ctx.lineTo(x + width, y + height - c);
    ctx.lineTo(x + width - c, y + height);
    ctx.lineTo(x + c, y + height);
    ctx.lineTo(x, y + height - c);
    ctx.lineTo(x, y + c);
    ctx.closePath();
  }

  function fillPanel(x, y, width, height, fill, stroke = null, cut = 8, lineWidth = 1) {
    clippedPath(x, y, width, height, cut);
    ctx.fillStyle = fill;
    ctx.fill();
    if (stroke) {
      ctx.strokeStyle = stroke;
      ctx.lineWidth = lineWidth;
      ctx.stroke();
    }
  }

  function withClip(x, y, width, height, cut, draw) {
    ctx.save();
    clippedPath(x, y, width, height, cut);
    ctx.clip();
    draw();
    ctx.restore();
  }

  function resetWorld() {
    state.elapsed = 0;
    state.score = 0;
    state.collected = 0;
    state.health = 3;
    state.pulseCooldown = 0;
    state.pulseEnergy = 100;
    state.invulnerable = 0;
    state.shake = 0;
    state.flash = 0;
    state.rng = 19;
    state.player.x = W / 2;
    state.player.y = H / 2 + 78;
    state.player.vx = 0;
    state.player.vy = 0;
    state.player.angle = 0;
    state.hazards = [];
    state.particles = [];
    state.pulses = [];
    seedMeadow();
    for (let i = 0; i < 4; i += 1) spawnHazard(i);
    spawnSignal();
  }

  function seedMeadow() {
    state.meadow = [];
    state.flowers = [];
    state.stones = [];
    state.fireflies = [];

    for (let i = 0; i < 52; i += 1) {
      state.meadow.push({
        x: PLAY.left + 10 + rand() * (PLAY.right - PLAY.left - 20),
        y: PLAY.top + 12 + rand() * (PLAY.bottom - PLAY.top - 24),
        height: 4 + rand() * 8,
        lean: (rand() - 0.5) * 0.5,
        tone: rand() > 0.5 ? "#52705A" : "#45634F",
      });
    }

    for (let i = 0; i < 25; i += 1) {
      state.flowers.push({
        x: PLAY.left + 12 + rand() * (PLAY.right - PLAY.left - 24),
        y: PLAY.top + 22 + rand() * (PLAY.bottom - PLAY.top - 42),
        scale: 0.6 + rand() * 0.7,
        color: rand() > 0.5 ? COLORS.peach : COLORS.lavender,
        phase: rand() * TAU,
      });
    }

    for (let i = 0; i < 13; i += 1) {
      state.stones.push({
        x: PLAY.left + 12 + rand() * (PLAY.right - PLAY.left - 24),
        y: PLAY.top + 20 + rand() * (PLAY.bottom - PLAY.top - 44),
        scale: 0.65 + rand() * 0.8,
      });
    }

    for (let i = 0; i < 28; i += 1) {
      state.fireflies.push({
        x: PLAY.left + rand() * (PLAY.right - PLAY.left),
        y: PLAY.top + rand() * (PLAY.bottom - PLAY.top),
        phase: rand() * TAU,
        speed: 0.8 + rand() * 1.2,
      });
    }
  }

  function spawnSignal() {
    const margin = 76;
    let candidate;
    do {
      candidate = {
        x: PLAY.left + margin + rand() * (PLAY.right - PLAY.left - margin * 2),
        y: PLAY.top + margin + rand() * (PLAY.bottom - PLAY.top - margin * 2),
        r: 11,
        phase: rand() * TAU,
      };
    } while (distance(candidate, state.player) < 130);
    state.signal = candidate;
  }

  function spawnHazard(index = 0) {
    const edge = Math.floor(rand() * 4);
    const speed = 25 + rand() * 24 + state.collected * 1.8;
    let x = PLAY.left + rand() * (PLAY.right - PLAY.left);
    let y = PLAY.top + rand() * (PLAY.bottom - PLAY.top);
    if (edge === 0) x = PLAY.left + 22;
    if (edge === 1) x = PLAY.right - 22;
    if (edge === 2) y = PLAY.top + 22;
    if (edge === 3) y = PLAY.bottom - 22;
    const angle = rand() * TAU;
    state.hazards.push({
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      r: 12 + rand() * 8,
      spin: (rand() - 0.5) * 2.4,
      rotation: rand() * TAU,
      phase: rand() * TAU,
      kind: rand() > 0.7 ? "horn" : "slime",
      id: `nightfall-${index}-${Math.floor(rand() * 10000)}`,
    });
  }

  function addParticles(x, y, color, count = 10, force = 90) {
    for (let i = 0; i < count; i += 1) {
      const angle = rand() * TAU;
      const speed = 20 + rand() * force;
      state.particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        life: 0.35 + rand() * 0.7,
        maxLife: 1,
        size: 1 + rand() * 2.5,
        color,
      });
    }
  }

  function startGame() {
    resetWorld();
    state.mode = "playing";
  }

  function restartGame() {
    startGame();
  }

  function togglePause() {
    if (state.mode === "playing") state.mode = "paused";
    else if (state.mode === "paused") state.mode = "playing";
  }

  function emitPulse() {
    if (state.mode !== "playing" || state.pulseCooldown > 0 || state.pulseEnergy < 25) return;
    state.pulseCooldown = 1.35;
    state.pulseEnergy -= 25;
    state.pulses.push({ x: state.player.x, y: state.player.y, radius: 16, life: 0.38 });
    state.shake = 0.12;
    addParticles(state.player.x, state.player.y, COLORS.mint, 20, 150);
    state.hazards = state.hazards.filter((hazard) => {
      const hit = distance(hazard, state.player) < 152;
      if (hit) {
        state.score += 25;
        addParticles(hazard.x, hazard.y, COLORS.gold, 14, 120);
      }
      return !hit;
    });
  }

  function finish(mode) {
    state.mode = mode;
    state.player.vx = 0;
    state.player.vy = 0;
    if (mode === "won") addParticles(state.player.x, state.player.y, COLORS.gold, 42, 220);
  }

  function update(dt) {
    if (state.mode !== "playing") {
      updateParticles(dt * 0.65);
      return;
    }

    state.elapsed += dt;
    state.pulseCooldown = Math.max(0, state.pulseCooldown - dt);
    state.pulseEnergy = Math.min(100, state.pulseEnergy + dt * 4.6);
    state.invulnerable = Math.max(0, state.invulnerable - dt);
    state.shake = Math.max(0, state.shake - dt);
    state.flash = Math.max(0, state.flash - dt);

    const left = keys.has("ArrowLeft") || keys.has("a");
    const right = keys.has("ArrowRight") || keys.has("d");
    const up = keys.has("ArrowUp") || keys.has("w");
    const down = keys.has("ArrowDown") || keys.has("s");
    const ax = (right ? 1 : 0) - (left ? 1 : 0);
    const ay = (down ? 1 : 0) - (up ? 1 : 0);
    const acceleration = 700;
    const maxSpeed = 230;
    state.player.vx += ax * acceleration * dt;
    state.player.vy += ay * acceleration * dt;
    state.player.vx *= Math.pow(0.0009, dt);
    state.player.vy *= Math.pow(0.0009, dt);
    const speed = Math.hypot(state.player.vx, state.player.vy);
    if (speed > maxSpeed) {
      state.player.vx = (state.player.vx / speed) * maxSpeed;
      state.player.vy = (state.player.vy / speed) * maxSpeed;
    }
    state.player.x = clamp(state.player.x + state.player.vx * dt, PLAY.left + 20, PLAY.right - 20);
    state.player.y = clamp(state.player.y + state.player.vy * dt, PLAY.top + 20, PLAY.bottom - 20);
    if (speed > 8) state.player.angle = Math.atan2(state.player.vy, state.player.vx) * 0.12;

    for (const firefly of state.fireflies) {
      firefly.phase += dt * firefly.speed;
      firefly.y -= Math.sin(firefly.phase * 0.7) * dt * 2;
    }

    for (const hazard of state.hazards) {
      hazard.x += hazard.vx * dt;
      hazard.y += hazard.vy * dt;
      hazard.rotation += hazard.spin * dt;
      hazard.phase += dt * 2;
      if (hazard.x < PLAY.left + hazard.r || hazard.x > PLAY.right - hazard.r) hazard.vx *= -1;
      if (hazard.y < PLAY.top + hazard.r || hazard.y > PLAY.bottom - hazard.r) hazard.vy *= -1;
      hazard.x = clamp(hazard.x, PLAY.left + hazard.r, PLAY.right - hazard.r);
      hazard.y = clamp(hazard.y, PLAY.top + hazard.r, PLAY.bottom - hazard.r);
      if (state.invulnerable <= 0 && distance(hazard, state.player) < hazard.r + state.player.r - 3) {
        state.health -= 1;
        state.invulnerable = 1.2;
        state.flash = 0.25;
        state.shake = 0.28;
        addParticles(state.player.x, state.player.y, COLORS.coral, 18, 150);
        state.player.x = W / 2;
        state.player.y = H / 2 + 78;
        state.player.vx = 0;
        state.player.vy = 0;
        if (state.health <= 0) finish("lost");
      }
    }

    if (state.signal && distance(state.signal, state.player) < state.signal.r + state.player.r + 3) {
      state.collected += 1;
      state.score += 100 + Math.max(0, Math.floor((ROUND_SECONDS - state.elapsed) * 2));
      addParticles(state.signal.x, state.signal.y, COLORS.mint, 20, 140);
      if (state.collected >= TARGET) finish("won");
      else {
        spawnSignal();
        if (state.hazards.length < 7) spawnHazard(state.collected + state.hazards.length);
      }
    }

    for (const pulse of state.pulses) {
      pulse.radius += 360 * dt;
      pulse.life -= dt;
    }
    state.pulses = state.pulses.filter((pulse) => pulse.life > 0);
    updateParticles(dt);

    if (ROUND_SECONDS - state.elapsed <= 0) finish("lost");
  }

  function updateParticles(dt) {
    for (const particle of state.particles) {
      particle.x += particle.vx * dt;
      particle.y += particle.vy * dt;
      particle.vx *= Math.pow(0.04, dt);
      particle.vy *= Math.pow(0.04, dt);
      particle.life -= dt;
    }
    state.particles = state.particles.filter((particle) => particle.life > 0);
  }

  function drawBackground() {
    const sky = ctx.createLinearGradient(0, 0, 0, H);
    sky.addColorStop(0, COLORS.nightViolet);
    sky.addColorStop(0.42, "#263448");
    sky.addColorStop(1, COLORS.meadowDeep);
    ctx.fillStyle = sky;
    ctx.fillRect(0, 0, W, H);

    ctx.save();
    ctx.globalAlpha = 0.2;
    ctx.fillStyle = COLORS.lavender;
    ctx.beginPath();
    ctx.arc(814, 82, 44, 0, TAU);
    ctx.fill();
    ctx.globalAlpha = 0.92;
    ctx.fillStyle = COLORS.parchment;
    ctx.beginPath();
    ctx.arc(814, 82, 25, 0, TAU);
    ctx.fill();
    ctx.fillStyle = COLORS.nightViolet;
    ctx.beginPath();
    ctx.arc(826, 73, 25, 0, TAU);
    ctx.fill();
    ctx.restore();

    ctx.fillStyle = "#1E2A35";
    ctx.beginPath();
    ctx.moveTo(0, 166);
    ctx.quadraticCurveTo(150, 116, 300, 167);
    ctx.quadraticCurveTo(470, 104, 648, 166);
    ctx.quadraticCurveTo(804, 112, 960, 163);
    ctx.lineTo(960, 252);
    ctx.lineTo(0, 252);
    ctx.closePath();
    ctx.fill();

    fillPanel(
      PLAY.left,
      PLAY.top,
      PLAY.right - PLAY.left,
      PLAY.bottom - PLAY.top,
      COLORS.meadow,
      "rgba(246, 232, 189, 0.22)",
      14,
      2,
    );

    withClip(PLAY.left, PLAY.top, PLAY.right - PLAY.left, PLAY.bottom - PLAY.top, 14, () => {
      ctx.fillStyle = "rgba(142, 227, 194, 0.06)";
      for (let i = 0; i < 9; i += 1) {
        ctx.beginPath();
        ctx.ellipse(110 + i * 105, 180 + (i % 2) * 90, 78, 30, -0.16, 0, TAU);
        ctx.fill();
      }

      ctx.strokeStyle = "rgba(255, 242, 201, 0.06)";
      ctx.lineWidth = 2;
      ctx.setLineDash([3, 11]);
      ctx.beginPath();
      ctx.moveTo(PLAY.left - 20, 330);
      ctx.quadraticCurveTo(240, 280, 425, 380);
      ctx.quadraticCurveTo(630, 488, PLAY.right + 20, 370);
      ctx.stroke();
      ctx.setLineDash([]);

      for (const grass of state.meadow) {
        ctx.save();
        ctx.translate(grass.x, grass.y);
        ctx.rotate(grass.lean);
        ctx.strokeStyle = grass.tone;
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(-2, -grass.height);
        ctx.moveTo(0, 0);
        ctx.lineTo(3, -grass.height - 2);
        ctx.stroke();
        ctx.restore();
      }

      for (const stone of state.stones) drawStone(stone.x, stone.y, stone.scale);
      for (const flower of state.flowers) drawFlower(flower);

      for (const firefly of state.fireflies) {
        const alpha = 0.22 + (Math.sin(firefly.phase) + 1) * 0.22;
        ctx.globalAlpha = alpha;
        ctx.fillStyle = COLORS.gold;
        ctx.beginPath();
        ctx.arc(firefly.x, firefly.y, 1.5 + alpha * 1.2, 0, TAU);
        ctx.fill();
      }
      ctx.globalAlpha = 1;
    });

    ctx.fillStyle = "rgba(33, 25, 43, 0.22)";
    ctx.fillRect(0, 0, W, 78);
  }

  function drawStone(x, y, scale) {
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(scale, scale);
    ctx.fillStyle = "rgba(33, 25, 43, 0.22)";
    ctx.beginPath();
    ctx.ellipse(0, 5, 10, 4, 0, 0, TAU);
    ctx.fill();
    ctx.fillStyle = "#71816F";
    ctx.strokeStyle = "#31433B";
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(-9, 2);
    ctx.lineTo(-5, -5);
    ctx.lineTo(3, -7);
    ctx.lineTo(9, -2);
    ctx.lineTo(6, 5);
    ctx.lineTo(-5, 6);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.restore();
  }

  function drawFlower(flower) {
    const bob = Math.sin(state.elapsed * 1.4 + flower.phase) * 1.2;
    ctx.save();
    ctx.translate(flower.x, flower.y + bob);
    ctx.scale(flower.scale, flower.scale);
    ctx.strokeStyle = "#6C8C5B";
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(0, 6);
    ctx.lineTo(0, -3);
    ctx.stroke();
    ctx.fillStyle = flower.color;
    for (let i = 0; i < 4; i += 1) {
      ctx.save();
      ctx.rotate(i * Math.PI / 2);
      ctx.beginPath();
      ctx.ellipse(0, -5, 2.8, 5, 0, 0, TAU);
      ctx.fill();
      ctx.restore();
    }
    ctx.fillStyle = COLORS.gold;
    ctx.beginPath();
    ctx.arc(0, 0, 2.2, 0, TAU);
    ctx.fill();
    ctx.restore();
  }

  function drawHud() {
    ctx.save();
    ctx.textBaseline = "alphabetic";

    withClip(22, 14, 54, 54, 8, () => {
      ctx.fillStyle = COLORS.timber;
      ctx.fillRect(22, 14, 54, 54);
      ctx.fillStyle = "rgba(246, 232, 189, 0.18)";
      ctx.fillRect(28, 20, 42, 42);
      drawCourier(49, 48, 0.74, 0, 0, false);
    });
    clippedPath(22, 14, 54, 54, 8);
    ctx.strokeStyle = COLORS.parchment;
    ctx.lineWidth = 2;
    ctx.stroke();

    ctx.textAlign = "left";
    ctx.fillStyle = COLORS.cream;
    ctx.font = `700 13px ${UI_FONT}`;
    ctx.fillText("MEADOW COURIER", 87, 30);
    ctx.fillStyle = COLORS.coral;
    ctx.font = `700 19px ${UI_FONT}`;
    ctx.fillText("♥".repeat(state.health), 87, 55);
    ctx.fillStyle = "rgba(255, 242, 201, 0.26)";
    ctx.fillText("♥".repeat(3 - state.health), 87 + state.health * 18, 55);

    ctx.textAlign = "center";
    ctx.fillStyle = COLORS.muted;
    ctx.font = `700 10px ${UI_FONT}`;
    ctx.fillText("NIGHTFALL", W / 2, 27);
    ctx.fillStyle = COLORS.cream;
    ctx.font = `700 25px ${DISPLAY_FONT}`;
    ctx.fillText(formatTime(state.elapsed), W / 2, 57);

    ctx.textAlign = "right";
    ctx.fillStyle = COLORS.muted;
    ctx.font = `700 10px ${UI_FONT}`;
    ctx.fillText("MOON DEW", W - 30, 27);
    ctx.fillStyle = COLORS.mint;
    ctx.font = `700 17px ${UI_FONT}`;
    ctx.fillText(`${String(state.collected).padStart(2, "0")} / ${TARGET}`, W - 30, 48);
    ctx.fillStyle = COLORS.muted;
    ctx.font = `700 10px ${UI_FONT}`;
    ctx.fillText(`SCORE  ${String(state.score).padStart(4, "0")}`, W - 30, 65);

    drawLoadout();
    drawXpBar();
    ctx.restore();
  }

  function drawLoadout() {
    const slotSize = 38;
    const gap = 7;
    const total = slotSize * 5 + gap * 4;
    const startX = W / 2 - total / 2;
    const y = H - 67;
    for (let i = 0; i < 5; i += 1) {
      const x = startX + i * (slotSize + gap);
      const active = i === 0;
      fillPanel(
        x,
        y,
        slotSize,
        slotSize,
        active ? "rgba(111, 73, 56, 0.95)" : "rgba(33, 25, 43, 0.52)",
        active ? COLORS.gold : "rgba(246, 232, 189, 0.24)",
        6,
        active ? 2 : 1,
      );
      if (active) drawPulseIcon(x + slotSize / 2, y + 17, 0.86);
      else {
        ctx.fillStyle = "rgba(246, 232, 189, 0.28)";
        ctx.font = `700 16px ${UI_FONT}`;
        ctx.textAlign = "center";
        ctx.fillText("·", x + slotSize / 2, y + 23);
      }
    }
    ctx.textAlign = "center";
    ctx.fillStyle = COLORS.parchment;
    ctx.font = `700 9px ${UI_FONT}`;
    ctx.fillText("PROTECTIVE PULSE", W / 2, H - 22);
  }

  function drawPulseIcon(x, y, scale) {
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(scale, scale);
    ctx.strokeStyle = COLORS.mint;
    ctx.lineWidth = 2;
    ctx.globalAlpha = 0.88;
    ctx.beginPath();
    ctx.arc(0, 0, 11, 0.18, Math.PI - 0.18);
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(0, 0, 11, Math.PI + 0.18, TAU - 0.18);
    ctx.stroke();
    ctx.globalAlpha = 1;
    ctx.fillStyle = COLORS.gold;
    ctx.beginPath();
    ctx.moveTo(0, -7);
    ctx.lineTo(5, 0);
    ctx.lineTo(0, 7);
    ctx.lineTo(-5, 0);
    ctx.closePath();
    ctx.fill();
    ctx.restore();
  }

  function drawXpBar() {
    const x = PLAY.left;
    const y = H - 12;
    const width = PLAY.right - PLAY.left;
    const progress = state.collected / TARGET;
    ctx.fillStyle = "rgba(33, 25, 43, 0.75)";
    ctx.fillRect(x, y, width, 5);
    ctx.fillStyle = COLORS.mint;
    ctx.fillRect(x, y, width * progress, 5);
    ctx.fillStyle = COLORS.gold;
    ctx.fillRect(x + width * progress - (progress > 0 ? 2 : 0), y - 1, progress > 0 ? 4 : 0, 7);
  }

  function drawSignal() {
    if (!state.signal) return;
    const signal = state.signal;
    const pulse = 1 + Math.sin(state.elapsed * 4 + signal.phase) * 0.1;
    ctx.save();
    ctx.translate(signal.x, signal.y);
    ctx.scale(pulse, pulse);
    ctx.globalAlpha = 0.22;
    ctx.fillStyle = COLORS.mint;
    ctx.beginPath();
    ctx.arc(0, 0, 26, 0, TAU);
    ctx.fill();
    ctx.globalAlpha = 1;
    ctx.shadowColor = COLORS.mint;
    ctx.shadowBlur = 16;
    ctx.strokeStyle = COLORS.cream;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(0, -15);
    ctx.lineTo(12, 0);
    ctx.lineTo(0, 15);
    ctx.lineTo(-12, 0);
    ctx.closePath();
    ctx.stroke();
    ctx.shadowBlur = 0;
    ctx.fillStyle = COLORS.mint;
    ctx.beginPath();
    ctx.moveTo(0, -10);
    ctx.lineTo(8, 0);
    ctx.lineTo(0, 10);
    ctx.lineTo(-8, 0);
    ctx.closePath();
    ctx.fill();
    ctx.fillStyle = COLORS.white;
    ctx.beginPath();
    ctx.arc(0, 0, 3.5, 0, TAU);
    ctx.fill();
    ctx.restore();
  }

  function drawHazards() {
    for (const hazard of state.hazards) {
      if (hazard.kind === "horn") drawMoonhorn(hazard);
      else drawDuskSlime(hazard);
    }
  }

  function drawDuskSlime(hazard) {
    const bob = Math.sin(hazard.phase) * 2;
    ctx.save();
    ctx.translate(hazard.x, hazard.y + bob);
    ctx.rotate(hazard.rotation * 0.05);
    ctx.fillStyle = "rgba(33, 25, 43, 0.3)";
    ctx.beginPath();
    ctx.ellipse(0, hazard.r * 0.72, hazard.r * 0.9, hazard.r * 0.35, 0, 0, TAU);
    ctx.fill();
    ctx.fillStyle = "#8B70C7";
    ctx.strokeStyle = COLORS.nightInk;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(-hazard.r, 5);
    ctx.quadraticCurveTo(-hazard.r * 1.1, -hazard.r * 0.72, -hazard.r * 0.4, -hazard.r * 0.9);
    ctx.quadraticCurveTo(0, -hazard.r * 1.18, hazard.r * 0.42, -hazard.r * 0.82);
    ctx.quadraticCurveTo(hazard.r * 1.1, -hazard.r * 0.62, hazard.r, 5);
    ctx.quadraticCurveTo(hazard.r * 0.68, hazard.r * 0.86, 0, hazard.r * 0.78);
    ctx.quadraticCurveTo(-hazard.r * 0.68, hazard.r * 0.86, -hazard.r, 5);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = COLORS.white;
    ctx.beginPath();
    ctx.arc(-4, -2, 2.3, 0, TAU);
    ctx.arc(4, -2, 2.3, 0, TAU);
    ctx.fill();
    ctx.fillStyle = COLORS.nightInk;
    ctx.beginPath();
    ctx.arc(-4, -2, 1, 0, TAU);
    ctx.arc(4, -2, 1, 0, TAU);
    ctx.fill();
    ctx.restore();
  }

  function drawMoonhorn(hazard) {
    const bob = Math.sin(hazard.phase) * 1.5;
    ctx.save();
    ctx.translate(hazard.x, hazard.y + bob);
    ctx.rotate(hazard.rotation * 0.04);
    ctx.fillStyle = "rgba(33, 25, 43, 0.3)";
    ctx.beginPath();
    ctx.ellipse(0, hazard.r * 0.8, hazard.r, hazard.r * 0.32, 0, 0, TAU);
    ctx.fill();
    ctx.fillStyle = "#A65F55";
    ctx.strokeStyle = COLORS.nightInk;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(-hazard.r * 0.92, 5);
    ctx.quadraticCurveTo(-hazard.r * 0.96, -hazard.r * 0.7, -hazard.r * 0.45, -hazard.r * 0.64);
    ctx.lineTo(-hazard.r * 0.72, -hazard.r * 1.1);
    ctx.lineTo(-hazard.r * 0.25, -hazard.r * 0.72);
    ctx.quadraticCurveTo(0, -hazard.r * 0.94, hazard.r * 0.25, -hazard.r * 0.72);
    ctx.lineTo(hazard.r * 0.72, -hazard.r * 1.1);
    ctx.lineTo(hazard.r * 0.45, -hazard.r * 0.64);
    ctx.quadraticCurveTo(hazard.r * 0.96, -hazard.r * 0.7, hazard.r * 0.92, 5);
    ctx.quadraticCurveTo(hazard.r * 0.62, hazard.r * 0.84, 0, hazard.r * 0.76);
    ctx.quadraticCurveTo(-hazard.r * 0.62, hazard.r * 0.84, -hazard.r * 0.92, 5);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = COLORS.peach;
    ctx.beginPath();
    ctx.arc(-4, -1, 2, 0, TAU);
    ctx.arc(4, -1, 2, 0, TAU);
    ctx.fill();
    ctx.fillStyle = COLORS.white;
    ctx.beginPath();
    ctx.arc(-4, -1, 0.9, 0, TAU);
    ctx.arc(4, -1, 0.9, 0, TAU);
    ctx.fill();
    ctx.restore();
  }

  function drawCourier(x, y, scale = 1, angle = 0, bob = 0, showShadow = true) {
    ctx.save();
    ctx.translate(x, y + bob);
    ctx.rotate(angle);
    ctx.scale(scale, scale);
    if (showShadow) {
      ctx.fillStyle = "rgba(33, 25, 43, 0.34)";
      ctx.beginPath();
      ctx.ellipse(0, 19, 14, 5, 0, 0, TAU);
      ctx.fill();
    }

    ctx.fillStyle = COLORS.lavender;
    ctx.strokeStyle = COLORS.nightInk;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(-12, 4);
    ctx.quadraticCurveTo(-18, 12, -11, 19);
    ctx.lineTo(0, 12);
    ctx.lineTo(10, 19);
    ctx.quadraticCurveTo(18, 12, 12, 4);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = COLORS.cream;
    ctx.beginPath();
    ctx.roundRect?.(-10, -1, 20, 21, 5);
    if (!ctx.roundRect) {
      ctx.moveTo(-10, 3);
      ctx.lineTo(-10, 15);
      ctx.quadraticCurveTo(-10, 20, -5, 20);
      ctx.lineTo(5, 20);
      ctx.quadraticCurveTo(10, 20, 10, 15);
      ctx.lineTo(10, 3);
      ctx.closePath();
    }
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = "#C97A64";
    ctx.beginPath();
    ctx.arc(0, -12, 12, 0, TAU);
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = COLORS.nightInk;
    ctx.beginPath();
    ctx.arc(-4, -13, 1.9, 0, TAU);
    ctx.arc(4, -13, 1.9, 0, TAU);
    ctx.fill();
    ctx.fillStyle = COLORS.cream;
    ctx.beginPath();
    ctx.arc(-3.5, -13.5, 0.7, 0, TAU);
    ctx.arc(4.5, -13.5, 0.7, 0, TAU);
    ctx.fill();

    ctx.strokeStyle = COLORS.coral;
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(-10, -3);
    ctx.quadraticCurveTo(0, 5, 11, -3);
    ctx.stroke();
    ctx.strokeStyle = COLORS.gold;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(0, 4);
    ctx.lineTo(0, 14);
    ctx.stroke();
    ctx.restore();
  }

  function drawPlayer() {
    if (state.invulnerable > 0 && Math.floor(state.invulnerable * 14) % 2 === 0) return;
    const speed = Math.hypot(state.player.vx, state.player.vy);
    const bob = speed > 8 ? Math.sin(state.elapsed * 14) * 1.8 : Math.sin(state.elapsed * 2.2) * 0.8;
    ctx.save();
    if (speed > 8) {
      ctx.shadowColor = COLORS.mint;
      ctx.shadowBlur = 12;
    }
    drawCourier(state.player.x, state.player.y, 1.08, state.player.angle, bob, true);
    ctx.restore();
  }

  function drawEffects() {
    for (const pulse of state.pulses) {
      ctx.globalAlpha = clamp(pulse.life / 0.38, 0, 1);
      ctx.strokeStyle = COLORS.mint;
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(pulse.x, pulse.y, pulse.radius, 0, TAU);
      ctx.stroke();
      ctx.globalAlpha = 1;
    }
    for (const particle of state.particles) {
      ctx.globalAlpha = clamp(particle.life / particle.maxLife, 0, 1);
      ctx.fillStyle = particle.color;
      ctx.beginPath();
      ctx.arc(particle.x, particle.y, particle.size, 0, TAU);
      ctx.fill();
    }
    ctx.globalAlpha = 1;
  }

  function drawOverlay(title, subtitle, hint) {
    ctx.fillStyle = "rgba(23, 21, 43, 0.78)";
    ctx.fillRect(0, 0, W, H);

    const panelX = 170;
    const panelY = 132;
    const panelW = 620;
    const panelH = 334;
    fillPanel(panelX + 7, panelY + 8, panelW, panelH, "rgba(33, 25, 43, 0.5)", null, 12);
    fillPanel(panelX, panelY, panelW, panelH, COLORS.timber, COLORS.nightInk, 12, 3);
    fillPanel(panelX + 10, panelY + 10, panelW - 20, panelH - 20, COLORS.parchment, COLORS.gold, 8, 2);

    drawFlower({ x: panelX + 54, y: panelY + 52, scale: 1.1, color: COLORS.coral, phase: 0 });
    drawFlower({ x: panelX + panelW - 54, y: panelY + 52, scale: 1.1, color: COLORS.lavender, phase: 0.8 });

    ctx.textAlign = "center";
    ctx.fillStyle = COLORS.inkSoft;
    ctx.font = `700 11px ${UI_FONT}`;
    ctx.fillText("NIGHTFALL MEADOW  /  STORY RUN", W / 2, panelY + 48);
    ctx.fillStyle = COLORS.nightInk;
    ctx.font = `700 ${title.length > 16 ? 34 : 42}px ${DISPLAY_FONT}`;
    ctx.fillText(title, W / 2, panelY + 115);
    ctx.fillStyle = COLORS.inkSoft;
    ctx.font = `16px ${UI_FONT}`;
    ctx.fillText(subtitle, W / 2, panelY + 155);

    ctx.fillStyle = COLORS.timber;
    ctx.font = `700 15px ${UI_FONT}`;
    ctx.fillText(hint, W / 2, panelY + 225);
    ctx.fillStyle = COLORS.inkSoft;
    ctx.font = `12px ${UI_FONT}`;
    ctx.fillText("WASD / 方向鍵  移動     SPACE  脈衝     P  暫停     F  全螢幕", W / 2, panelY + 264);
    ctx.fillStyle = COLORS.muted;
    ctx.font = `11px ${UI_FONT}`;
    ctx.fillText("一個溫柔的小生活，仍會在夜色變得擁擠時繼續。", W / 2, panelY + 291);
    ctx.textAlign = "left";
  }

  function render() {
    ctx.save();
    if (state.shake > 0) {
      ctx.translate((rand() - 0.5) * state.shake * 18, (rand() - 0.5) * state.shake * 18);
    }
    drawBackground();
    drawHud();
    drawSignal();
    drawHazards();
    drawPlayer();
    drawEffects();
    if (state.mode === "menu") {
      drawOverlay("NIGHTFALL MEADOW", "Meadow Courier 的月露回收小徑。", "按 ENTER 或點擊開始");
    } else if (state.mode === "paused") {
      drawOverlay("REST AT THE LANTERN", "草葉停下來了，夜色也在等你。", "按 P 繼續旅程");
    } else if (state.mode === "won") {
      drawOverlay("MEADOW SAFE", `你收集了 ${TARGET} 枚月露，獲得 ${state.score} 分。`, "按 R 或點擊再走一趟");
    } else if (state.mode === "lost") {
      drawOverlay("NIGHT HAS TEETH", `這次帶回了 ${state.collected} / ${TARGET} 枚月露。`, "按 R 或點擊重新出發");
    }
    if (state.flash > 0) {
      ctx.fillStyle = `rgba(255, 117, 143, ${state.flash * 0.7})`;
      ctx.fillRect(0, 0, W, H);
    }
    ctx.restore();
  }

  function renderGameToText() {
    return JSON.stringify({
      coordinate_system: "origin top-left; x right; y down",
      theme: "Nightfall Meadow",
      mode: state.mode,
      player: {
        x: Math.round(state.player.x),
        y: Math.round(state.player.y),
        vx: Math.round(state.player.vx),
        vy: Math.round(state.player.vy),
        radius: state.player.r,
      },
      signal: state.signal ? { x: Math.round(state.signal.x), y: Math.round(state.signal.y), radius: state.signal.r } : null,
      hazards: state.hazards.map((hazard) => ({
        x: Math.round(hazard.x),
        y: Math.round(hazard.y),
        radius: Math.round(hazard.r),
        kind: hazard.kind,
      })),
      score: state.score,
      signals: `${state.collected}/${TARGET}`,
      integrity: state.health,
      pulse: { energy: Math.round(state.pulseEnergy), cooldown: Number(state.pulseCooldown.toFixed(2)) },
      time_left: Number(Math.max(0, ROUND_SECONDS - state.elapsed).toFixed(1)),
    });
  }

  window.render_game_to_text = renderGameToText;
  window.advanceTime = (ms) => {
    const steps = Math.max(1, Math.round(ms / (1000 / 60)));
    for (let i = 0; i < steps; i += 1) update(1 / 60);
    render();
  };

  function toggleFullscreen() {
    if (!document.fullscreenElement) canvas.requestFullscreen?.();
    else document.exitFullscreen?.();
  }

  canvas.addEventListener("pointerdown", () => {
    if (state.mode === "menu") startGame();
    else if (state.mode === "paused") togglePause();
    else if (state.mode === "won" || state.mode === "lost") restartGame();
  });

  window.addEventListener("keydown", (event) => {
    const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
    if (["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", " "].includes(event.key)) event.preventDefault();
    if ((key === "Enter" || key === " " || key === "Spacebar") && state.mode === "menu") startGame();
    else if (key === "p") togglePause();
    else if (key === "r" && ["won", "lost"].includes(state.mode)) restartGame();
    else if (key === "f") toggleFullscreen();
    else if (key === " " || key === "Spacebar") emitPulse();
    keys.add(key);
  });

  window.addEventListener("keyup", (event) => {
    const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
    keys.delete(key);
  });

  window.addEventListener("blur", () => keys.clear());

  document.addEventListener("fullscreenchange", () => {
    document.body.classList.toggle("fullscreen", Boolean(document.fullscreenElement));
  });

  resetWorld();
  let previous = performance.now();
  function frame(now) {
    const dt = clamp((now - previous) / 1000, 0, 0.05);
    previous = now;
    update(dt);
    render();
    requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
})();
