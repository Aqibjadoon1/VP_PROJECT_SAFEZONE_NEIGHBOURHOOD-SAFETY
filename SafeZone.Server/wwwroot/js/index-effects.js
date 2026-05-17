// Three.js Molecular Structure — SafeZone Brand
window.initHeroEffects = function () {
    if (typeof THREE === 'undefined') return;

    var container = document.getElementById('hero-canvas');
    if (!container) return;
    if (!container.offsetParent) return;

    var existingCanvas = container.querySelector('canvas');
    if (existingCanvas) existingCanvas.remove();

    var w = container.clientWidth;
    var h = container.clientHeight;
    if (w === 0 || h === 0) return;

    var scene = new THREE.Scene();
    var camera = new THREE.PerspectiveCamera(45, w / h, 0.1, 100);
    camera.position.set(0, 0.5, 5);
    camera.lookAt(0, 0, 0);

    var renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(w, h);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    container.appendChild(renderer.domElement);

    // === Build molecular structure using EdgesGeometry ===
    var baseGeo = new THREE.IcosahedronGeometry(1.4, 1);
    var edgesGeo = new THREE.EdgesGeometry(baseGeo);
    var edgePos = edgesGeo.attributes.position;

    // === Bonds (lines from edge pairs) ===
    var bondMat = new THREE.LineBasicMaterial({
        color: 0x00ff88,
        transparent: true,
        opacity: 0.4
    });

    var uniqueVerts = {};
    for (var i = 0; i < edgePos.count; i += 2) {
        var ax = Math.round(edgePos.getX(i) * 1000) / 1000;
        var ay = Math.round(edgePos.getY(i) * 1000) / 1000;
        var az = Math.round(edgePos.getZ(i) * 1000) / 1000;
        var bx = Math.round(edgePos.getX(i + 1) * 1000) / 1000;
        var by = Math.round(edgePos.getY(i + 1) * 1000) / 1000;
        var bz = Math.round(edgePos.getZ(i + 1) * 1000) / 1000;

        var keyA = ax + ',' + ay + ',' + az;
        var keyB = bx + ',' + by + ',' + bz;
        uniqueVerts[keyA] = { x: ax, y: ay, z: az };
        uniqueVerts[keyB] = { x: bx, y: by, z: bz };

        var bondGeo = new THREE.BufferGeometry();
        bondGeo.setAttribute('position', new THREE.BufferAttribute(
            new Float32Array([ax, ay, az, bx, by, bz]), 3
        ));
        scene.add(new THREE.Line(bondGeo, bondMat));
    }

    // === Nodes (atoms at unique vertices) ===
    var nodeMat = new THREE.MeshPhysicalMaterial({
        color: 0x00ff88,
        roughness: 0.2,
        metalness: 0.1,
        emissive: 0x00ff88,
        emissiveIntensity: 0.3,
        transparent: true,
        opacity: 0.85
    });

    var nodeGroup = new THREE.Group();
    var vertKeys = Object.keys(uniqueVerts);
    for (var v = 0; v < vertKeys.length; v++) {
        var p = uniqueVerts[vertKeys[v]];
        var node = new THREE.Mesh(new THREE.SphereGeometry(0.09, 12, 12), nodeMat);
        node.position.set(p.x, p.y, p.z);
        nodeGroup.add(node);
    }
    scene.add(nodeGroup);

    // === Inner small nodes ===
    var innerGeo = new THREE.IcosahedronGeometry(0.95, 1);
    var innerVerts = new THREE.EdgesGeometry(innerGeo).attributes.position;
    var innerGroup = new THREE.Group();
    var innerMat = new THREE.MeshPhysicalMaterial({
        color: 0x00ff88,
        roughness: 0.3,
        metalness: 0.05,
        emissive: 0x00ff88,
        emissiveIntensity: 0.15,
        transparent: true,
        opacity: 0.4
    });
    var seen = {};
    for (var i = 0; i < innerVerts.count; i++) {
        var x = Math.round(innerVerts.getX(i) * 1000) / 1000;
        var y = Math.round(innerVerts.getY(i) * 1000) / 1000;
        var z = Math.round(innerVerts.getZ(i) * 1000) / 1000;
        var k = x + ',' + y + ',' + z;
        if (!seen[k]) {
            seen[k] = true;
            var n = new THREE.Mesh(new THREE.SphereGeometry(0.045, 8, 8), innerMat);
            n.position.set(x, y, z);
            innerGroup.add(n);
        }
    }
    scene.add(innerGroup);

    // === Outer faint frame ===
    var outerGeo = new THREE.IcosahedronGeometry(1.7, 1);
    var outerEdges = new THREE.EdgesGeometry(outerGeo);
    var outerLineMat = new THREE.LineBasicMaterial({
        color: 0x00ff88,
        transparent: true,
        opacity: 0.07
    });
    scene.add(new THREE.LineSegments(outerEdges, outerLineMat));

    // === Orbiting Rings ===
    var ringMat = new THREE.MeshBasicMaterial({
        color: 0x00ff88,
        transparent: true,
        opacity: 0.2,
        side: THREE.DoubleSide
    });
    var ring = new THREE.Mesh(new THREE.TorusGeometry(2.0, 0.015, 16, 64), ringMat);
    ring.rotation.x = Math.PI / 3;
    ring.rotation.z = Math.PI / 6;
    scene.add(ring);

    var ring2 = new THREE.Mesh(new THREE.TorusGeometry(2.15, 0.01, 16, 64), ringMat.clone());
    ring2.material.opacity = 0.1;
    ring2.rotation.x = -Math.PI / 4;
    ring2.rotation.z = Math.PI / 4;
    scene.add(ring2);

    // === Orbiting Particles ===
    var pCount = 180;
    var pGeo = new THREE.BufferGeometry();
    var pPos = new Float32Array(pCount * 3);
    var pSpeeds = new Float32Array(pCount);
    var pRadii = new Float32Array(pCount);

    for (var i = 0; i < pCount; i++) {
        var r = 2.0 + Math.random() * 1.2;
        var a = Math.random() * Math.PI * 2;
        pRadii[i] = r;
        pSpeeds[i] = 0.15 + Math.random() * 0.25;
        pPos[i * 3] = Math.cos(a) * r;
        pPos[i * 3 + 1] = (Math.random() - 0.5) * 3.5;
        pPos[i * 3 + 2] = Math.sin(a) * r;
    }
    pGeo.setAttribute('position', new THREE.BufferAttribute(pPos, 3));

    var pMat = new THREE.PointsMaterial({
        color: 0x00ff88,
        size: 0.03,
        transparent: true,
        opacity: 0.45,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var particles = new THREE.Points(pGeo, pMat);
    scene.add(particles);

    // === Glow Halo ===
    var haloMat = new THREE.SpriteMaterial({
        map: (function () {
            var c = document.createElement('canvas');
            c.width = 128; c.height = 128;
            var ctx = c.getContext('2d');
            var g = ctx.createRadialGradient(64, 64, 0, 64, 64, 64);
            g.addColorStop(0, 'rgba(0,255,136,0.2)');
            g.addColorStop(0.3, 'rgba(0,255,136,0.06)');
            g.addColorStop(1, 'rgba(0,255,136,0)');
            ctx.fillStyle = g;
            ctx.fillRect(0, 0, 128, 128);
            return new THREE.CanvasTexture(c);
        })(),
        transparent: true,
        blending: THREE.AdditiveBlending,
        depthWrite: false
    });
    var halo = new THREE.Sprite(haloMat);
    halo.scale.set(6, 6, 1);
    halo.position.z = -0.5;
    scene.add(halo);

    // === Mouse ===
    var mouseX = 0, mouseY = 0, targetX = 0, targetY = 0;
    document.addEventListener('mousemove', function (e) {
        mouseX = (e.clientX / window.innerWidth) * 2 - 1;
        mouseY = -(e.clientY / window.innerHeight) * 2 + 1;
    });

    var pp = particles.geometry.attributes.position.array;
    var pAngles = new Float32Array(pCount);
    for (var i = 0; i < pCount; i++) {
        pAngles[i] = Math.atan2(pp[i * 3 + 2], pp[i * 3]);
    }

    // === Animation with damping for smooth rotation ===
    var time = 0;
    var rotY = 0, rotX = 0, targetRotY = 0, targetRotX = 0;
    var innerRotY = 0, innerRotX = 0;
    var ringRotY = 0, ring2RotY = 0;
    var dampFactor = 0.05;

    function animate() {
        requestAnimationFrame(animate);
        time += 0.008;

        targetX += (mouseX - targetX) * 0.025;
        targetY += (mouseY - targetY) * 0.025;

        targetRotY += 0.004;
        targetRotX += 0.001;
        rotY += (targetRotY - rotY) * dampFactor;
        rotX += (targetRotX - rotX) * dampFactor;
        nodeGroup.rotation.y = rotY;
        nodeGroup.rotation.x = rotX;

        innerRotY -= 0.003;
        innerRotX += 0.001;
        innerGroup.rotation.y += (innerRotY - innerGroup.rotation.y) * dampFactor;
        innerGroup.rotation.x += (innerRotX - innerGroup.rotation.x) * dampFactor;

        ringRotY += 0.005;
        ring2RotY -= 0.004;
        ring.rotation.y += (ringRotY - ring.rotation.y) * dampFactor;
        ring2.rotation.y += (ring2RotY - ring2.rotation.y) * dampFactor;

        for (var i = 0; i < pCount; i++) {
            pAngles[i] += pSpeeds[i] * 0.008;
            var r = pRadii[i];
            pp[i * 3] = Math.cos(pAngles[i]) * r;
            pp[i * 3 + 2] = Math.sin(pAngles[i]) * r;
            pp[i * 3 + 1] += Math.sin(time * 2 + i) * 0.001;
        }
        particles.geometry.attributes.position.needsUpdate = true;

        var ca = targetX * 0.35;
        var ch = 0.5 + targetY * 0.3;
        var cx = Math.sin(ca) * 6;
        var cz = Math.cos(ca) * 6;
        camera.position.x += (cx - camera.position.x) * 0.04;
        camera.position.y += (ch - camera.position.y) * 0.04;
        camera.position.z += (cz - camera.position.z) * 0.04;
        camera.lookAt(0, 0, 0);

        renderer.render(scene, camera);
    }
    animate();

    window.addEventListener('resize', function () {
        var nw = container.clientWidth;
        var nh = container.clientHeight;
        if (nw === 0 || nh === 0) return;
        camera.aspect = nw / nh;
        camera.updateProjectionMatrix();
        renderer.setSize(nw, nh);
    });
};

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

(function () {
    var counters = document.querySelectorAll('.counter[data-target]');
    if (!counters.length) return;

    function animateCounter(element) {
        var target = parseInt(element.getAttribute('data-target'));
        var duration = 1500;
        var start = performance.now();

        function update(currentTime) {
            var elapsed = currentTime - start;
            var progress = Math.min(elapsed / duration, 1);
            var easeProgress = 1 - (1 - progress) * (1 - progress);
            var current = Math.floor(easeProgress * target);
            element.textContent = current + (target === 24 ? '/7' : '');
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
})();
