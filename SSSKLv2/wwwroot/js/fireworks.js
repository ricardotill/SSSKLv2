// Simple fireworks implementation
// Exposes window.fireworks.start() and window.fireworks.stop()
(function () {
    if (window.fireworks) return; // already loaded

    const FW = {};
    let canvas = null;
    let ctx = null;
    let w = 0, h = 0;
    let rafId = null;
    let running = false;
    let fireworks = [];
    let particles = [];
    let lastLaunch = 0;
    const STYLE_ID = 'ssskv2-fireworks-style';

    function ensureStyle() {
        if (document.getElementById(STYLE_ID)) return;
        const s = document.createElement('style');
        s.id = STYLE_ID;
        s.type = 'text/css';
        // force pointer-events none even if other CSS tries to override
        // also set initial opacity and transition so we can fade in/out
        s.appendChild(document.createTextNode(`#ssskv2-fireworks-canvas{pointer-events: none !important; touch-action: none !important; user-select: none !important; -webkit-user-select: none !important; opacity: 0; transition: opacity 400ms ease; background: transparent !important;}`));
        document.head.appendChild(s);
    }

    function ensureCanvas() {
        if (canvas) return;
        ensureStyle();
        canvas = document.createElement('canvas');
        canvas.id = 'ssskv2-fireworks-canvas';
        // Use precise CSS; we'll still set style properties individually
        Object.assign(canvas.style, {
            position: 'fixed',
            left: '0',
            top: '0',
            right: '0',
            bottom: '0',
            width: '100%',
            height: '100%',
            // pointer-events is forced via injected stylesheet with !important above
            // Put the fireworks canvas under modal/backdrop (Bootstrap uses ~1050),
            // but high enough to show above most page content.
            zIndex: '1030',
            display: 'block',
            
            background: 'transparent'
         });
         // Make sure the canvas does not capture focus or accessibility events
         canvas.setAttribute('aria-hidden', 'true');
         canvas.setAttribute('role', 'presentation');
         canvas.setAttribute('tabindex', '-1');
         // ensure it really doesn't accept pointer events at DOM level
         canvas.style.pointerEvents = 'none';
         canvas.style.touchAction = 'none';
         canvas.style.userSelect = 'none';
         // start with zero opacity (style sheet also sets this) to allow fade-in
         canvas.style.opacity = '0';
         canvas.style.transition = 'opacity 400ms ease';

         document.body.appendChild(canvas);
         ctx = canvas.getContext('2d');
         resize();
         // ensure fully cleared initial frame to avoid residual black
         ctx && ctx.clearRect(0, 0, canvas.width, canvas.height);
         window.addEventListener('resize', resize);
     }

     function resize() {
         if (!canvas) return;
         const dpr = window.devicePixelRatio || 1;
         w = canvas.width = Math.floor(window.innerWidth * dpr);
         h = canvas.height = Math.floor(window.innerHeight * dpr);
         canvas.style.width = window.innerWidth + 'px';
         canvas.style.height = window.innerHeight + 'px';
         ctx && ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
     }

     function rand(min, max) { return Math.random() * (max - min) + min; }

     function createFirework() {
         const x = rand(50, window.innerWidth - 50);
         const y = window.innerHeight + 10;
         const targetY = rand(window.innerHeight * 0.2, window.innerHeight * 0.6);
         const speed = rand(6, 9);
         const hue = Math.floor(rand(0, 360));
         fireworks.push({ x, y, targetY, speed, hue });
     }

     function explode(fw) {
         const count = Math.floor(rand(20, 60));
         for (let i = 0; i < count; i++) {
             const angle = rand(0, Math.PI * 2);
             const speed = rand(1, 6);
             const vx = Math.cos(angle) * speed;
             const vy = Math.sin(angle) * speed;
             particles.push({ x: fw.x, y: fw.targetY, vx, vy, life: rand(40, 120), hue: fw.hue, alpha: 1 });
         }
     }

     function step() {
         if (!ctx) return;
        // semi-transparent background to create subtle trails; lowered alpha for subtlety
        ctx.globalCompositeOperation = 'source-over';
        ctx.fillStyle = 'rgba(0,0,0,0.025)';
        ctx.fillRect(0, 0, window.innerWidth, window.innerHeight);

         // update fireworks
         for (let i = fireworks.length - 1; i >= 0; i--) {
             const fw = fireworks[i];
             fw.y -= fw.speed;
             // draw
             ctx.beginPath();
             ctx.fillStyle = `hsl(${fw.hue},100%,60%)`;
             ctx.arc(fw.x, fw.y, 3, 0, Math.PI * 2);
             ctx.fill();
             if (fw.y <= fw.targetY) {
                 // explode
                 explode(fw);
                 fireworks.splice(i, 1);
             }
         }

         // update particles
         for (let i = particles.length - 1; i >= 0; i--) {
             const p = particles[i];
             p.vy += 0.02; // gravity
             p.x += p.vx;
             p.y += p.vy;
             p.vx *= 0.99; p.vy *= 0.99;
             p.life -= 1;
             p.alpha = Math.max(0, p.life / 120);

            ctx.beginPath();
            ctx.globalCompositeOperation = 'lighter';
            ctx.fillStyle = `hsla(${p.hue},100%,60%,${p.alpha})`;
            ctx.arc(p.x, p.y, 2.2, 0, Math.PI * 2);
            ctx.fill();

            if (p.life <= 0 || p.alpha <= 0) particles.splice(i, 1);
         }

         const now = Date.now();
         if (now - lastLaunch > 500 && fireworks.length < 5) {
             // launch a few fireworks
             const launches = Math.floor(rand(1, 3));
             for (let j = 0; j < launches; j++) createFirework();
             lastLaunch = now;
         }

         if (running) rafId = requestAnimationFrame(step);
     }

    FW.start = function () {
         if (running) return;
         ensureCanvas();
         // fade in the canvas for a smooth entrance
         requestAnimationFrame(() => {
            try {
                // clear first frame to avoid an initial black smear
                ctx && ctx.clearRect(0, 0, canvas.width, canvas.height);
                canvas.style.opacity = '1';
            } catch (e) { /* ignore */ }
         });
         running = true;
         lastLaunch = 0;
         // clear arrays
         fireworks = [];
         particles = [];
         // fill background transparent initially
         ctx && ctx.clearRect(0, 0, canvas.width, canvas.height);
         rafId = requestAnimationFrame(step);
     };
 
     FW.stop = function () {
        // fade out the canvas then remove it after the transition
        try {
            running = false;
            if (rafId) {
                cancelAnimationFrame(rafId);
                rafId = null;
            }
            fireworks = [];
            particles = [];
            if (canvas) {
                // start fade-out
                canvas.style.opacity = '0';
                // wait for transition to complete (~500ms) before removing
                const removeAfter = 550;
                setTimeout(() => {
                    try {
                        if (ctx && canvas) ctx.clearRect(0, 0, canvas.width, canvas.height);
                        window.removeEventListener('resize', resize);
                        if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
                    } catch (e) {
                        console.warn('Error removing fireworks canvas', e);
                    }
                    canvas = null;
                    ctx = null;
                    // remove injected style as well
                    try {
                        const s = document.getElementById(STYLE_ID);
                        if (s && s.parentNode) s.parentNode.removeChild(s);
                    } catch (e) {
                        console.warn('Error removing fireworks style', e);
                    }
                }, removeAfter);
            }
        } catch (e) {
            console.warn('Error stopping fireworks', e);
        }
     };

     window.fireworks = FW;
})();
