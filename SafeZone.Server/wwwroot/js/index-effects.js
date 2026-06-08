// Three.js green hologram core - SafeZone brand
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
    scene.fog = new THREE.FogExp2(0x04120c, 0.055);

    var camera = new THREE.PerspectiveCamera(54, w / h, 0.1, 100);
    camera.position.set(0, 0.45, 5.4);
    camera.lookAt(0, 0, 0);

    var renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(w, h);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.25;
    container.appendChild(renderer.domElement);

    scene.add(new THREE.AmbientLight(0x123626, 1.45));

    var keyLight = new THREE.DirectionalLight(0xaaffd4, 0.9);
    keyLight.position.set(2.6, 4.4, 4.2);
    scene.add(keyLight);

    var greenLight = new THREE.PointLight(0x00ff88, 4.8, 8);
    greenLight.position.set(-2.4, 1.6, 3.1);
    scene.add(greenLight);

    var rimLight = new THREE.PointLight(0x63ffd1, 2.1, 7);
    rimLight.position.set(2.3, -1.1, 2.7);
    scene.add(rimLight);

    var backLight = new THREE.PointLight(0x0bbf6a, 2.0, 7);
    backLight.position.set(0, 2.3, -3.2);
    scene.add(backLight);

    var hologram = new THREE.Group();
    hologram.rotation.z = -0.06;
    scene.add(hologram);

    var knotGeo = new THREE.TorusKnotGeometry(1.05, 0.31, 220, 28, 2, 3);
    var knotMat = new THREE.MeshBasicMaterial({
        color: 0x00ff88,
        transparent: true,
        opacity: 0.12,
        side: THREE.DoubleSide,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var knot = new THREE.Mesh(knotGeo, knotMat);
    hologram.add(knot);

    var wireMat = new THREE.MeshBasicMaterial({
        color: 0x00ff88,
        wireframe: true,
        transparent: true,
        opacity: 0.16,
        depthWrite: false
    });
    var wire = new THREE.Mesh(knotGeo, wireMat);
    hologram.add(wire);

    var shellGeo = new THREE.TorusKnotGeometry(1.1, 0.35, 220, 28, 2, 3);
    var shellMat = new THREE.MeshBasicMaterial({
        color: 0x18ff95,
        wireframe: true,
        transparent: true,
        opacity: 0.055,
        depthWrite: false
    });
    var shell = new THREE.Mesh(shellGeo, shellMat);
    hologram.add(shell);

    var glassMat = new THREE.MeshPhysicalMaterial({
        color: 0x0aff8d,
        emissive: 0x00bf68,
        emissiveIntensity: 0.12,
        roughness: 0.04,
        metalness: 0.03,
        clearcoat: 1,
        clearcoatRoughness: 0.04,
        transmission: 0.74,
        transparent: true,
        opacity: 0.08,
        side: THREE.DoubleSide,
        depthWrite: false
    });
    var glassOrb = new THREE.Mesh(new THREE.SphereGeometry(1.58, 96, 48), glassMat);
    hologram.add(glassOrb);

    function makeGlowTexture() {
        var c = document.createElement('canvas');
        c.width = 256;
        c.height = 256;
        var ctx = c.getContext('2d');
        var g = ctx.createRadialGradient(128, 128, 2, 128, 128, 128);
        g.addColorStop(0, 'rgba(0,255,136,0.32)');
        g.addColorStop(0.25, 'rgba(45,255,192,0.18)');
        g.addColorStop(0.58, 'rgba(0,255,136,0.07)');
        g.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, 256, 256);
        return new THREE.CanvasTexture(c);
    }

    var innerGlowMat = new THREE.SpriteMaterial({
        map: makeGlowTexture(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false,
        opacity: 0.7
    });
    var innerGlow = new THREE.Sprite(innerGlowMat);
    innerGlow.scale.set(3.5, 3.5, 1);
    hologram.add(innerGlow);

    var haloMat = new THREE.SpriteMaterial({
        map: makeGlowTexture(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false,
        opacity: 0.52
    });
    var halo = new THREE.Sprite(haloMat);
    halo.scale.set(5.6, 5.6, 1);
    halo.position.z = -0.45;
    scene.add(halo);

    var pCount = 1200;
    var pGeo = new THREE.BufferGeometry();
    var positions = new Float32Array(pCount * 3);
    var speeds = new Float32Array(pCount);
    var radii = new Float32Array(pCount);
    var angles = new Float32Array(pCount);

    for (var pi = 0; pi < pCount; pi++) {
        var radius = 1.55 + Math.random() * 1.75;
        var theta = Math.random() * Math.PI * 2;
        var phi = Math.acos(2 * Math.random() - 1);
        radii[pi] = radius;
        angles[pi] = theta;
        speeds[pi] = 0.05 + Math.random() * 0.18;
        positions[pi * 3] = radius * Math.sin(phi) * Math.cos(theta);
        positions[pi * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
        positions[pi * 3 + 2] = radius * Math.cos(phi);
    }
    pGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3));

    var pMat = new THREE.PointsMaterial({
        color: 0x00ff88,
        size: 0.019,
        transparent: true,
        opacity: 0.42,
        sizeAttenuation: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var particles = new THREE.Points(pGeo, pMat);
    hologram.add(particles);

    function makeRing(radius, tilt, color, opacity) {
        var geo = new THREE.TorusGeometry(radius, 0.006, 8, 160);
        var mat = new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: opacity,
            blending: THREE.AdditiveBlending,
            depthWrite: false
        });
        var mesh = new THREE.Mesh(geo, mat);
        mesh.rotation.x = tilt;
        hologram.add(mesh);
        return mesh;
    }

    var ring1 = makeRing(1.85, Math.PI / 2, 0x00ff88, 0.34);
    var ring2 = makeRing(2.16, Math.PI / 3.1, 0x63ffd1, 0.2);
    var ring3 = makeRing(2.48, -Math.PI / 5.2, 0x0bbf6a, 0.14);

    function makeGridDisc() {
        var geo = new THREE.BufferGeometry();
        var verts = [];
        var spokes = 32;
        var rings = 5;
        var outerRadius = 2.55;

        for (var ri = 1; ri <= rings; ri++) {
            var rad = (ri / rings) * outerRadius;
            var seg = 96;
            for (var si = 0; si < seg; si++) {
                var a0 = (si / seg) * Math.PI * 2;
                var a1 = ((si + 1) / seg) * Math.PI * 2;
                verts.push(rad * Math.cos(a0), 0, rad * Math.sin(a0));
                verts.push(rad * Math.cos(a1), 0, rad * Math.sin(a1));
            }
        }

        for (var sp = 0; sp < spokes; sp++) {
            var a = (sp / spokes) * Math.PI * 2;
            verts.push(0, 0, 0);
            verts.push(outerRadius * Math.cos(a), 0, outerRadius * Math.sin(a));
        }

        geo.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3));
        var mat = new THREE.LineBasicMaterial({
            color: 0x00ff88,
            transparent: true,
            opacity: 0.1,
            blending: THREE.AdditiveBlending,
            depthWrite: false
        });
        return new THREE.LineSegments(geo, mat);
    }

    var disc = makeGridDisc();
    disc.rotation.x = Math.PI / 2;
    disc.position.y = -0.08;
    hologram.add(disc);

    var mouseX = 0;
    var mouseY = 0;
    var targetX = 0;
    var targetY = 0;
    var mouseHandler = function (e) {
        mouseX = (e.clientX / window.innerWidth) * 2 - 1;
        mouseY = -(e.clientY / window.innerHeight) * 2 + 1;
    };
    document.addEventListener('mousemove', mouseHandler);

    var time = 0;
    var disposed = false;
    var animationFrame = 0;
    var pp = particles.geometry.attributes.position.array;

    function animate() {
        if (disposed) return;
        animationFrame = requestAnimationFrame(animate);
        time += 0.01;

        targetX += (mouseX - targetX) * 0.025;
        targetY += (mouseY - targetY) * 0.025;

        hologram.rotation.y += 0.0048;
        hologram.rotation.x = Math.sin(time * 0.28) * 0.07;
        hologram.position.y = Math.sin(time * 0.45) * 0.06;

        knot.rotation.x = time * 0.22;
        knot.rotation.y = time * 0.31;
        wire.rotation.copy(knot.rotation);
        shell.rotation.x = time * 0.18;
        shell.rotation.y = -time * 0.25;
        glassOrb.rotation.y = -time * 0.06;

        var breathe = 0.5 + 0.5 * Math.sin(time * 1.35);
        knotMat.opacity = 0.055 + breathe * 0.055;
        wireMat.opacity = 0.11 + breathe * 0.08;
        shellMat.opacity = 0.035 + breathe * 0.055;
        glassMat.opacity = 0.06 + breathe * 0.035;
        innerGlowMat.opacity = 0.48 + breathe * 0.18;
        pMat.opacity = 0.28 + breathe * 0.18;

        ring1.rotation.z = time * 0.2;
        ring2.rotation.z = -time * 0.15;
        ring2.rotation.y = time * 0.08;
        ring3.rotation.x = -Math.PI / 5.2 + time * 0.1;
        ring3.rotation.z = time * 0.12;
        disc.rotation.z = time * 0.08;

        for (var i = 0; i < pCount; i++) {
            angles[i] += speeds[i] * 0.002;
            var verticalPulse = Math.sin(time * 1.2 + i * 0.04) * 0.0025;
            pp[i * 3] += Math.cos(angles[i]) * 0.0009;
            pp[i * 3 + 1] += verticalPulse;
            pp[i * 3 + 2] += Math.sin(angles[i]) * 0.0009;
            var distance = Math.sqrt(
                pp[i * 3] * pp[i * 3] +
                pp[i * 3 + 1] * pp[i * 3 + 1] +
                pp[i * 3 + 2] * pp[i * 3 + 2]
            );
            if (distance > radii[i] + 0.08 || distance < radii[i] - 0.08) {
                var scale = radii[i] / Math.max(distance, 0.0001);
                pp[i * 3] *= scale;
                pp[i * 3 + 1] *= scale;
                pp[i * 3 + 2] *= scale;
            }
        }
        particles.geometry.attributes.position.needsUpdate = true;

        greenLight.position.x = 2.6 * Math.cos(time * 0.5);
        greenLight.position.z = 2.6 * Math.sin(time * 0.5);
        rimLight.position.x = -2.4 * Math.sin(time * 0.42);
        rimLight.position.z = 2.4 * Math.cos(time * 0.42);

        var ca = targetX * 0.28;
        var ch = 0.65 + targetY * 0.22;
        var cx = Math.sin(ca) * 5.4;
        var cz = Math.cos(ca) * 5.4;
        camera.position.x += (cx - camera.position.x) * 0.03;
        camera.position.y += (ch - camera.position.y) * 0.03;
        camera.position.z += (cz - camera.position.z) * 0.03;
        camera.lookAt(0, Math.sin(time * 0.3) * 0.04, 0);

        renderer.render(scene, camera);
    }
    animate();

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
