// Three.js liquid hologram orb - SafeZone brand
window.initHeroEffects = function () {
    var container = document.getElementById('hero-canvas');
    if (!container) return;

    if (typeof THREE === 'undefined') {
        scheduleHeroRetry();
        return;
    }

    if (!container.offsetParent && window.getComputedStyle(container).display === 'none') {
        scheduleHeroRetry();
        return;
    }

    if (container.__safezoneHeroCleanup) {
        container.__safezoneHeroCleanup();
    }

    var existingCanvas = container.querySelector('canvas');
    if (existingCanvas) existingCanvas.remove();

    var w = container.clientWidth || container.offsetWidth;
    var h = container.clientHeight || container.offsetHeight;
    if (w === 0 || h === 0) {
        scheduleHeroRetry();
        return;
    }

    window.__safezoneHeroRetryCount = 0;

    var scene = new THREE.Scene();
    var camera = new THREE.PerspectiveCamera(50, w / h, 0.1, 100);
    camera.position.set(0, 0.65, 5.8);
    camera.lookAt(0, 0, 0);

    var renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(w, h);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;
    container.appendChild(renderer.domElement);

    scene.add(new THREE.AmbientLight(0x9af7ff, 0.58));
    var keyLight = new THREE.DirectionalLight(0xffffff, 1.42);
    keyLight.position.set(2.6, 4.6, 4.2);
    scene.add(keyLight);
    var greenLight = new THREE.PointLight(0x2dffc0, 3.8, 8);
    greenLight.position.set(-2.7, 1.8, 3.2);
    scene.add(greenLight);
    var rimLight = new THREE.PointLight(0x50e8ff, 2.7, 7);
    rimLight.position.set(2.4, -1.2, 2.8);
    scene.add(rimLight);
    var backLight = new THREE.PointLight(0x7d61ff, 2.0, 7);
    backLight.position.set(0, 2.4, -3.4);
    scene.add(backLight);

    // === Blue-violet liquid hologram orb ===
    var orbGroup = new THREE.Group();
    orbGroup.rotation.z = -0.05;
    scene.add(orbGroup);

    var orbRadius = 1.62;

    var shellMat = new THREE.MeshPhysicalMaterial({
        color: 0x182fff,
        emissive: 0x1f77ff,
        emissiveIntensity: 0.18,
        roughness: 0.025,
        metalness: 0.02,
        clearcoat: 1,
        clearcoatRoughness: 0.025,
        transmission: 0.82,
        transparent: true,
        opacity: 0.22,
        side: THREE.DoubleSide,
        depthWrite: false
    });

    var shell = new THREE.Mesh(new THREE.SphereGeometry(orbRadius, 112, 56), shellMat);
    orbGroup.add(shell);

    var rimShellMat = new THREE.MeshBasicMaterial({
        color: 0x6b4dff,
        transparent: true,
        opacity: 0.16,
        blending: THREE.AdditiveBlending,
        side: THREE.BackSide,
        depthWrite: false
    });
    var rimShell = new THREE.Mesh(new THREE.SphereGeometry(orbRadius * 1.03, 96, 48), rimShellMat);
    orbGroup.add(rimShell);

    var innerGlowMat = new THREE.SpriteMaterial({
        map: (function () {
            var c = document.createElement('canvas');
            c.width = 256; c.height = 256;
            var ctx = c.getContext('2d');
            var g = ctx.createRadialGradient(128, 128, 4, 128, 128, 128);
            g.addColorStop(0, 'rgba(120,78,255,0.52)');
            g.addColorStop(0.28, 'rgba(42,119,255,0.28)');
            g.addColorStop(0.58, 'rgba(0,255,156,0.1)');
            g.addColorStop(1, 'rgba(0,0,0,0)');
            ctx.fillStyle = g;
            ctx.fillRect(0, 0, 256, 256);
            return new THREE.CanvasTexture(c);
        })(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false,
        opacity: 0.78
    });
    var innerGlow = new THREE.Sprite(innerGlowMat);
    innerGlow.scale.set(3.4, 3.4, 1);
    orbGroup.add(innerGlow);

    function createSurfaceSwirlCurve(phase, twist, yScale, radius, frequency) {
        var pts = [];
        for (var i = 0; i <= 170; i++) {
            var t = i / 170;
            var latitude = Math.sin(t * Math.PI * 2 * frequency + phase) * yScale +
                Math.sin(t * Math.PI * 7 + phase * 0.45) * 0.12;
            latitude = Math.max(-0.88, Math.min(0.88, latitude));
            var ring = Math.sqrt(Math.max(0.04, 1 - latitude * latitude));
            var a = t * Math.PI * 2 * twist + phase +
                Math.sin(t * Math.PI * 5 + phase) * 0.62;
            var wobble = 1 + Math.sin(t * Math.PI * 10 + phase) * 0.035;
            pts.push(new THREE.Vector3(
                Math.cos(a) * radius * ring * wobble,
                latitude * radius,
                Math.sin(a) * radius * ring * wobble
            ));
        }
        return new THREE.CatmullRomCurve3(pts);
    }

    function createSwirlMaterial(color, opacity) {
        return new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: opacity,
            blending: THREE.AdditiveBlending,
            depthWrite: false
        });
    }

    var surfaceSwirls = [];
    var swirlColors = [0x22dfff, 0x2f66ff, 0x704dff, 0x9a4dff, 0x00ff9f, 0x32c8ff, 0x345bff, 0x7d58ff, 0x27ffc8, 0x5a82ff, 0xc64dff, 0x5e4dff];
    for (var sw = 0; sw < 15; sw++) {
        var phase = sw * 0.53;
        var twist = 0.85 + (sw % 5) * 0.28;
        var yScale = 0.44 + (sw % 4) * 0.09;
        var tubeRadius = sw % 5 === 0 ? 0.031 : 0.017 + (sw % 3) * 0.003;
        var frequency = 1 + (sw % 3) * 0.45;
        var swirlMesh = new THREE.Mesh(
            new THREE.TubeGeometry(createSurfaceSwirlCurve(phase, twist, yScale, orbRadius * (0.96 + (sw % 3) * 0.012), frequency), 170, tubeRadius, 12, false),
            createSwirlMaterial(swirlColors[sw % swirlColors.length], sw % 5 === 0 ? 0.56 : 0.36)
        );
        swirlMesh.rotation.x = (sw % 5 - 2) * 0.22;
        swirlMesh.rotation.y = phase * 0.27;
        swirlMesh.rotation.z = (sw % 2 === 0 ? 1 : -1) * (0.16 + sw * 0.008);
        orbGroup.add(swirlMesh);
        surfaceSwirls.push({
            mesh: swirlMesh,
            material: swirlMesh.material,
            phase: phase,
            baseOpacity: sw % 5 === 0 ? 0.45 : 0.25,
            speed: (sw % 2 === 0 ? 1 : -1) * (0.001 + sw * 0.00007)
        });
    }

    var sparkleMat = new THREE.MeshBasicMaterial({
        color: 0x53fff0,
        transparent: true,
        opacity: 0.48,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var orbSparkles = [];
    for (var gl = 0; gl < 34; gl++) {
        var theta = Math.random() * Math.PI * 2;
        var u = Math.random() * 2 - 1;
        var ring = Math.sqrt(1 - u * u);
        var sparkle = new THREE.Mesh(new THREE.SphereGeometry(0.01 + Math.random() * 0.014, 8, 8), sparkleMat.clone());
        sparkle.position.set(
            Math.cos(theta) * ring * orbRadius * 1.01,
            u * orbRadius * 1.01,
            Math.sin(theta) * ring * orbRadius * 1.01
        );
        orbGroup.add(sparkle);
        orbSparkles.push({
            mesh: sparkle,
            phase: Math.random() * Math.PI * 2,
            baseOpacity: 0.25 + Math.random() * 0.45
        });
    }

    // === Glow halo behind hologram ===
    var haloMat = new THREE.SpriteMaterial({
        map: (function () {
            var c = document.createElement('canvas');
            c.width = 256; c.height = 256;
            var ctx = c.getContext('2d');
            var g = ctx.createRadialGradient(128, 128, 0, 128, 128, 128);
            g.addColorStop(0, 'rgba(126,96,255,0.2)');
            g.addColorStop(0.22, 'rgba(45,255,192,0.12)');
            g.addColorStop(0.52, 'rgba(45,168,255,0.05)');
            g.addColorStop(1, 'rgba(0,0,0,0)');
            ctx.fillStyle = g;
            ctx.fillRect(0, 0, 256, 256);
            return new THREE.CanvasTexture(c);
        })(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var halo = new THREE.Sprite(haloMat);
    halo.scale.set(5.5, 5.5, 1);
    halo.position.z = -0.3;
    scene.add(halo);

    // === Orbiting Particles ===
    var pCount = 250;
    var pGeo = new THREE.BufferGeometry();
    var pPos = new Float32Array(pCount * 3);
    var pSpeeds = new Float32Array(pCount);
    var pRadii = new Float32Array(pCount);
    var pYOffset = new Float32Array(pCount);

    for (var i = 0; i < pCount; i++) {
        var r = 2.4 + Math.random() * 1.8;
        var a = Math.random() * Math.PI * 2;
        pRadii[i] = r;
        pSpeeds[i] = 0.08 + Math.random() * 0.2;
        pYOffset[i] = (Math.random() - 0.5) * 4.5;
        pPos[i * 3] = Math.cos(a) * r;
        pPos[i * 3 + 1] = pYOffset[i];
        pPos[i * 3 + 2] = Math.sin(a) * r;
    }
    pGeo.setAttribute('position', new THREE.BufferAttribute(pPos, 3));

    var pMat = new THREE.PointsMaterial({
        color: 0x45f7ff,
        size: 0.025,
        transparent: true,
        opacity: 0.5,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var particles = new THREE.Points(pGeo, pMat);
    scene.add(particles);

    // === Floating code/matrix symbols ===
    var symbolGroup = new THREE.Group();
    scene.add(symbolGroup);
    var symbols = '0123456789SOSALERTSAFEZONE';
    var symbolMat = new THREE.SpriteMaterial({
        map: (function () {
            var c = document.createElement('canvas');
            c.width = 64; c.height = 64;
            var ctx = c.getContext('2d');
            ctx.font = 'bold 32px "Courier New", monospace';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillStyle = 'rgba(92,226,255,0.12)';
            ctx.fillText('01', 32, 32);
            return new THREE.CanvasTexture(c);
        })(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });

    var symSprites = [];
    for (var i = 0; i < 15; i++) {
        var sMat = symbolMat.clone();
        var sprite = new THREE.Sprite(sMat);
        var radius = 2.2 + Math.random() * 2.0;
        var theta = Math.random() * Math.PI * 2;
        sprite.position.set(
            Math.cos(theta) * radius,
            (Math.random() - 0.5) * 4,
            Math.sin(theta) * radius
        );
        sprite.scale.set(0.15 + Math.random() * 0.2, 0.15 + Math.random() * 0.2, 1);
        sprite.material.opacity = 0.04 + Math.random() * 0.08;
        symbolGroup.add(sprite);
        symSprites.push({
            sprite: sprite,
            theta: theta,
            radius: radius,
            speed: 0.002 + Math.random() * 0.005,
            ySpeed: 0.001 + Math.random() * 0.003
        });
    }

    // === Mouse tracking ===
    var mouseX = 0, mouseY = 0, targetX = 0, targetY = 0;
    var mouseHandler = function (e) {
        mouseX = (e.clientX / window.innerWidth) * 2 - 1;
        mouseY = -(e.clientY / window.innerHeight) * 2 + 1;
    };
    document.addEventListener('mousemove', mouseHandler);

    // Particle animation data
    var pp = particles.geometry.attributes.position.array;
    var pAngles = new Float32Array(pCount);
    for (var i = 0; i < pCount; i++) {
        pAngles[i] = Math.atan2(pp[i * 3 + 2], pp[i * 3]);
    }

    // === Animation ===
    var time = 0;
    var targetRotY = 0, targetRotX = 0;
    var currentRotY = 0, currentRotX = 0;
    var hoverPulse = 0;
    var disposed = false;
    var animationFrame = 0;

    function animate() {
        if (disposed) return;
        animationFrame = requestAnimationFrame(animate);
        time += 0.01;
        hoverPulse += 0.02;

        targetX += (mouseX - targetX) * 0.02;
        targetY += (mouseY - targetY) * 0.02;

        targetRotY += 0.006;
        targetRotX = Math.sin(time * 0.3) * 0.05;
        currentRotY += (targetRotY - currentRotY) * 0.04;
        currentRotX += (targetRotX - currentRotX) * 0.04;

        // Gentle floating on the hologram orb.
        orbGroup.rotation.y = currentRotY;
        orbGroup.rotation.x = currentRotX + Math.sin(time * 0.5) * 0.02;
        orbGroup.position.y = Math.sin(time * 0.4) * 0.05;

        // Pulse the glass shell and liquid swirls so it reads like a live hologram.
        var pulse = 0.4 + Math.sin(hoverPulse) * 0.15;
        shellMat.opacity = 0.17 + pulse * 0.055;
        rimShellMat.opacity = 0.13 + pulse * 0.055;
        innerGlowMat.opacity = 0.62 + pulse * 0.14;
        shell.rotation.y -= 0.0018;
        shell.rotation.x = Math.sin(time * 0.34) * 0.035;
        rimShell.rotation.y += 0.0011;
        rimShell.rotation.x = -Math.sin(time * 0.28) * 0.025;
        for (var swi = 0; swi < surfaceSwirls.length; swi++) {
            var swirl = surfaceSwirls[swi];
            swirl.mesh.rotation.y += swirl.speed;
            swirl.mesh.rotation.x += Math.sin(time * 0.18 + swirl.phase) * 0.0006;
            swirl.material.opacity = swirl.baseOpacity + pulse * 0.09 +
                Math.sin(time * 1.2 + swirl.phase) * 0.025;
        }
        for (var gli = 0; gli < orbSparkles.length; gli++) {
            var sparkle = orbSparkles[gli];
            var sparklePulse = 0.65 + Math.sin(time * 2.1 + sparkle.phase) * 0.35;
            sparkle.mesh.scale.setScalar(0.75 + sparklePulse * 0.75);
            sparkle.mesh.material.opacity = sparkle.baseOpacity * sparklePulse;
        }

        // Orbiting particles
        for (var i = 0; i < pCount; i++) {
            pAngles[i] += pSpeeds[i] * 0.01;
            var r = pRadii[i];
            pp[i * 3] = Math.cos(pAngles[i]) * r;
            pp[i * 3 + 2] = Math.sin(pAngles[i]) * r;
            pp[i * 3 + 1] += Math.sin(time * 1.5 + i * 0.3) * 0.0008;
        }
        particles.geometry.attributes.position.needsUpdate = true;

        // Floating symbols rotate slowly
        symbolGroup.rotation.y += 0.001;
        symbolGroup.rotation.x = Math.sin(time * 0.2) * 0.03;

        // Camera follow mouse with parallax
        var ca = targetX * 0.3;
        var ch = 1 + targetY * 0.25;
        var cx = Math.sin(ca) * 6;
        var cz = Math.cos(ca) * 6;
        camera.position.x += (cx - camera.position.x) * 0.03;
        camera.position.y += (ch - camera.position.y) * 0.03;
        camera.position.z += (cz - camera.position.z) * 0.03;
        camera.lookAt(0, Math.sin(time * 0.3) * 0.05, 0);

        renderer.render(scene, camera);
    }
    animate();

    // Resize handler
    var resizeHandler = function () {
        var nw = container.clientWidth;
        var nh = container.clientHeight;
        if (nw === 0 || nh === 0) return;
        camera.aspect = nw / nh;
        camera.updateProjectionMatrix();
        renderer.setSize(nw, nh);
    };
    window.addEventListener('resize', resizeHandler);

    container.__safezoneHeroCleanup = function () {
        disposed = true;
        if (animationFrame) cancelAnimationFrame(animationFrame);
        document.removeEventListener('mousemove', mouseHandler);
        window.removeEventListener('resize', resizeHandler);
        renderer.dispose();
    };
};

function scheduleHeroRetry() {
    window.__safezoneHeroRetryCount = window.__safezoneHeroRetryCount || 0;
    if (window.__safezoneHeroRetryCount >= 30) return;
    window.__safezoneHeroRetryCount += 1;
    window.setTimeout(window.initHeroEffects, 100);
}

(function () {
    function tryInit() {
        window.initHeroEffects();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryInit);
    } else {
        tryInit();
    }
})();

window.initLandingCounters = function () {
    var counters = document.querySelectorAll('.counter[data-target]');
    if (!counters.length) return;

    function animateCounter(element) {
        if (element.dataset.counterAnimated === 'true') return;
        element.dataset.counterAnimated = 'true';

        var target = parseInt(element.getAttribute('data-target'), 10);
        if (Number.isNaN(target)) return;

        var duration = 1500;
        var start = performance.now();
        var suffix = target === 24 ? '/7' : '';

        function update(currentTime) {
            var elapsed = currentTime - start;
            var progress = Math.min(elapsed / duration, 1);
            var easeProgress = 1 - (1 - progress) * (1 - progress);
            var current = Math.floor(easeProgress * target);
            element.textContent = current + suffix;
            if (progress < 1) requestAnimationFrame(update);
        }
        requestAnimationFrame(update);
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                animateCounter(entry.target);
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.5 });

    counters.forEach(function (c) { observer.observe(c); });
};

(function () {
    function tryCounters() {
        window.initLandingCounters();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryCounters);
    } else {
        tryCounters();
    }
})();
