// Three.js DNA Helix Hologram — SafeZone Brand
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
    camera.position.set(0, 1, 6);
    camera.lookAt(0, 0, 0);

    var renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(w, h);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;
    container.appendChild(renderer.domElement);

    scene.add(new THREE.AmbientLight(0x88ffee, 0.55));
    var keyLight = new THREE.DirectionalLight(0xffffff, 1.35);
    keyLight.position.set(2.6, 4.6, 4.2);
    scene.add(keyLight);
    var cyanLight = new THREE.PointLight(0x00d4ff, 3.2, 8);
    cyanLight.position.set(-2.8, 1.8, 3.4);
    scene.add(cyanLight);
    var greenLight = new THREE.PointLight(0x00ff88, 2.7, 7);
    greenLight.position.set(2.4, -1.1, 2.6);
    scene.add(greenLight);
    var violetLight = new THREE.PointLight(0x8b5cf6, 1.8, 7);
    violetLight.position.set(0, 2.5, -3.4);
    scene.add(violetLight);

    // === Holographic DNA helix sculpture ===
    var helixGroup = new THREE.Group();
    helixGroup.rotation.z = -0.02;
    scene.add(helixGroup);

    var helixRadius = 0.86;
    var helixHeight = 4.05;
    var turns = 2.65;
    var segments = 184;
    var ribbonWidth = 0.28;

    var spineMatA = new THREE.MeshPhysicalMaterial({
        color: 0xeffff8,
        emissive: 0x00d4ff,
        emissiveIntensity: 0.42,
        roughness: 0.12,
        metalness: 0.52,
        clearcoat: 0.8,
        clearcoatRoughness: 0.15,
        transparent: true,
        opacity: 0.96
    });

    var spineMatB = new THREE.MeshPhysicalMaterial({
        color: 0x172f37,
        emissive: 0x00ff88,
        emissiveIntensity: 0.55,
        roughness: 0.1,
        metalness: 0.65,
        clearcoat: 0.75,
        clearcoatRoughness: 0.12,
        transparent: true,
        opacity: 0.93
    });

    var ribbonMat = new THREE.MeshBasicMaterial({
        color: 0x00d4ff,
        transparent: true,
        opacity: 0.13,
        side: THREE.DoubleSide,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });

    var rungMat = new THREE.MeshPhysicalMaterial({
        color: 0xdffff2,
        emissive: 0x00ff88,
        emissiveIntensity: 0.2,
        roughness: 0.2,
        metalness: 0.34,
        transparent: true,
        opacity: 0.78
    });

    var baseMat = new THREE.MeshPhysicalMaterial({
        color: 0x0e2025,
        emissive: 0x00d4ff,
        emissiveIntensity: 0.16,
        roughness: 0.16,
        metalness: 0.72,
        transparent: true,
        opacity: 0.88
    });

    var pulseMat = new THREE.MeshBasicMaterial({
        color: 0x00ff88,
        transparent: true,
        opacity: 0.9,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });

    var orbitMat = new THREE.MeshBasicMaterial({
        color: 0x00d4ff,
        transparent: true,
        opacity: 0.22,
        blending: THREE.AdditiveBlending,
        side: THREE.DoubleSide,
        depthWrite: false
    });

    function helixPoint(t, offset, radius) {
        var a = t * Math.PI * 2 * turns + offset;
        return new THREE.Vector3(
            Math.cos(a) * radius,
            -helixHeight / 2 + t * helixHeight,
            Math.sin(a) * radius
        );
    }

    function helixRadial(t, offset) {
        var a = t * Math.PI * 2 * turns + offset;
        return new THREE.Vector3(Math.cos(a), 0, Math.sin(a)).normalize();
    }

    function createHelixCurve(offset, radius) {
        var pts = [];
        for (var i = 0; i <= segments; i++) {
            pts.push(helixPoint(i / segments, offset, radius));
        }
        return new THREE.CatmullRomCurve3(pts);
    }

    function createRibbonGeometry(offset, width) {
        var positions = [];
        var uvs = [];
        var indices = [];

        for (var i = 0; i <= segments; i++) {
            var t = i / segments;
            var center = helixPoint(t, offset, helixRadius);
            var radial = helixRadial(t, offset).multiplyScalar(width / 2);
            var outer = center.clone().add(radial);
            var inner = center.clone().sub(radial);

            positions.push(outer.x, outer.y, outer.z, inner.x, inner.y, inner.z);
            uvs.push(0, t, 1, t);

            if (i < segments) {
                var a = i * 2;
                indices.push(a, a + 1, a + 2, a + 1, a + 3, a + 2);
            }
        }

        var geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
        geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
        geometry.setIndex(indices);
        geometry.computeVertexNormals();
        return geometry;
    }

    function cylinderBetween(start, end, radius, material) {
        var direction = new THREE.Vector3().subVectors(end, start);
        var length = direction.length();
        var mesh = new THREE.Mesh(new THREE.CylinderGeometry(radius, radius, length, 16), material);
        mesh.position.copy(start).add(end).multiplyScalar(0.5);
        mesh.quaternion.setFromUnitVectors(
            new THREE.Vector3(0, 1, 0),
            direction.clone().normalize()
        );
        return mesh;
    }

    var spineCurveA = createHelixCurve(0, helixRadius);
    var spineCurveB = createHelixCurve(Math.PI, helixRadius);
    var spineA = new THREE.Mesh(new THREE.TubeGeometry(spineCurveA, segments, 0.075, 18, false), spineMatA);
    var spineB = new THREE.Mesh(new THREE.TubeGeometry(spineCurveB, segments, 0.075, 18, false), spineMatB);
    helixGroup.add(spineA);
    helixGroup.add(spineB);

    var ribbonA = new THREE.Mesh(createRibbonGeometry(0, ribbonWidth), ribbonMat);
    var ribbonB = new THREE.Mesh(createRibbonGeometry(Math.PI, ribbonWidth), ribbonMat.clone());
    ribbonB.material.color.set(0x00ff88);
    ribbonB.material.opacity = 0.11;
    helixGroup.add(ribbonA);
    helixGroup.add(ribbonB);

    for (var r = 0; r < 28; r++) {
        var rt = 0.035 + r * (0.93 / 27);
        var start = helixPoint(rt, 0, helixRadius - 0.12);
        var end = helixPoint(rt, Math.PI, helixRadius - 0.12);
        var rung = cylinderBetween(start, end, 0.026, rungMat);
        helixGroup.add(rung);
    }

    var baseY = -helixHeight / 2 - 0.24;
    var base = new THREE.Mesh(new THREE.CylinderGeometry(1.12, 1.36, 0.24, 112), baseMat);
    base.position.y = baseY;
    helixGroup.add(base);

    var baseTop = new THREE.Mesh(
        new THREE.TorusGeometry(1.18, 0.035, 18, 128),
        new THREE.MeshBasicMaterial({ color: 0x00ff88, transparent: true, opacity: 0.42 })
    );
    baseTop.rotation.x = Math.PI / 2;
    baseTop.position.y = baseY + 0.15;
    helixGroup.add(baseTop);

    var baseGlow = new THREE.Mesh(
        new THREE.TorusGeometry(0.72, 0.016, 16, 128),
        new THREE.MeshBasicMaterial({ color: 0x00d4ff, transparent: true, opacity: 0.38, blending: THREE.AdditiveBlending })
    );
    baseGlow.rotation.x = Math.PI / 2;
    baseGlow.position.y = baseY + 0.19;
    helixGroup.add(baseGlow);

    var baseRim = new THREE.Mesh(
        new THREE.TorusGeometry(1.36, 0.045, 18, 128),
        new THREE.MeshBasicMaterial({ color: 0x02070a, transparent: true, opacity: 0.68 })
    );
    baseRim.rotation.x = Math.PI / 2;
    baseRim.position.y = baseY - 0.13;
    helixGroup.add(baseRim);

    var orbitRingA = new THREE.Mesh(new THREE.TorusGeometry(1.62, 0.008, 8, 160), orbitMat);
    orbitRingA.rotation.x = Math.PI / 2.65;
    orbitRingA.rotation.z = Math.PI / 7;
    helixGroup.add(orbitRingA);

    var orbitRingB = new THREE.Mesh(new THREE.TorusGeometry(1.88, 0.006, 8, 160), orbitMat.clone());
    orbitRingB.material.color.set(0x8b5cf6);
    orbitRingB.material.opacity = 0.16;
    orbitRingB.rotation.x = -Math.PI / 3.3;
    orbitRingB.rotation.z = -Math.PI / 5;
    helixGroup.add(orbitRingB);

    var pulseNodes = [];
    for (var pn = 0; pn < 8; pn++) {
        var pulseNode = new THREE.Mesh(new THREE.SphereGeometry(0.045, 12, 12), pulseMat.clone());
        helixGroup.add(pulseNode);
        pulseNodes.push({
            mesh: pulseNode,
            offset: pn % 2 === 0 ? 0 : Math.PI,
            phase: pn / 8
        });
    }

    // === Glow halo behind helix ===
    var haloMat = new THREE.SpriteMaterial({
        map: (function () {
            var c = document.createElement('canvas');
            c.width = 256; c.height = 256;
            var ctx = c.getContext('2d');
            var g = ctx.createRadialGradient(128, 128, 0, 128, 128, 128);
            g.addColorStop(0, 'rgba(0,255,136,0.15)');
            g.addColorStop(0.2, 'rgba(0,255,136,0.08)');
            g.addColorStop(0.5, 'rgba(0,255,136,0.03)');
            g.addColorStop(1, 'rgba(0,255,136,0)');
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
        color: 0x00ff88,
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
            ctx.fillStyle = 'rgba(0,255,136,0.12)';
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

        // Gentle floating on helix
        helixGroup.rotation.y = currentRotY;
        helixGroup.rotation.x = currentRotX + Math.sin(time * 0.5) * 0.02;
        helixGroup.position.y = Math.sin(time * 0.4) * 0.05;

        // Pulse the sculptural materials so the helix feels alive, not decorative.
        var pulse = 0.4 + Math.sin(hoverPulse) * 0.15;
        spineMatA.emissiveIntensity = 0.32 + pulse * 0.22;
        spineMatB.emissiveIntensity = 0.38 + pulse * 0.26;
        rungMat.emissiveIntensity = 0.12 + pulse * 0.14;
        baseMat.emissiveIntensity = 0.08 + pulse * 0.08;
        ribbonA.material.opacity = 0.11 + pulse * 0.055;
        ribbonB.material.opacity = 0.09 + pulse * 0.05;
        orbitRingA.rotation.z += 0.0024;
        orbitRingB.rotation.z -= 0.0018;
        baseGlow.rotation.z += 0.006;

        for (var pni = 0; pni < pulseNodes.length; pni++) {
            var pNode = pulseNodes[pni];
            var tPulse = (time * 0.12 + pNode.phase) % 1;
            pNode.mesh.position.copy(helixPoint(tPulse, pNode.offset, helixRadius));
            var scale = 0.65 + Math.sin((tPulse + pNode.phase) * Math.PI * 2) * 0.18;
            pNode.mesh.scale.setScalar(scale);
            pNode.mesh.material.opacity = 0.35 + (1 - Math.abs(0.5 - tPulse) * 2) * 0.55;
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
