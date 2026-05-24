(function (window) {
    'use strict';

    const maps = {};
    const markers = {};
    const heatmapLayers = {};
    const markerClusters = {};

    const DEFAULT_CENTER = { lat: 33.6844, lng: 73.0479 };
    const DEFAULT_ZOOM = 13;
    const TILE_URL = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png';
    const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>';

    function initMap(containerId, options = {}) {
        const element = document.getElementById(containerId);
        if (!element) {
            console.error(`Map container "${containerId}" was not found.`);
            return null;
        }

        if (!window.L) {
            console.error('Leaflet is not loaded. Check Leaflet CSS/JS links in _Host.cshtml.');
            return null;
        }

        const center = options.center || DEFAULT_CENTER;
        const zoom = options.zoom || DEFAULT_ZOOM;
        const L = window.L;

        if (maps[containerId]) {
            maps[containerId].setView([center.lat, center.lng], zoom);
            setTimeout(() => maps[containerId]?.invalidateSize(), 80);
            return maps[containerId];
        }

        const map = L.map(containerId, {
            zoomControl: options.zoomControl !== false,
            scrollWheelZoom: options.scrollWheelZoom !== false,
            zoomAnimation: true
        }).setView([center.lat, center.lng], zoom);

        L.tileLayer(TILE_URL, {
            attribution: TILE_ATTRIBUTION,
            maxZoom: 19
        }).addTo(map);

        maps[containerId] = map;
        markers[containerId] = [];
        setTimeout(() => map.invalidateSize(), 120);
        return map;
    }

    function getMap(containerId) {
        return maps[containerId];
    }

    function normalizeIncident(incident) {
        const lat = Number(incident.lat ?? incident.latitude ?? incident.Latitude);
        const lng = Number(incident.lng ?? incident.longitude ?? incident.Longitude);
        return {
            ...incident,
            lat,
            lng,
            title: incident.title ?? incident.Title ?? 'Incident',
            categoryName: incident.categoryName ?? incident.CategoryName ?? 'Incident',
            status: incident.status ?? incident.Status ?? 0,
            severity: incident.severity ?? incident.Severity ?? 1,
            reportedAt: incident.reportedAt ?? incident.ReportedAt
        };
    }

    function createMarker(lat, lng, options = {}) {
        const L = window.L;
        if (!L || !Number.isFinite(Number(lat)) || !Number.isFinite(Number(lng))) {
            return null;
        }

        const color = options.color || '#00FF88';
        const label = options.icon || '';
        const htmlIcon = L.divIcon({
            html: `<div style="
                width: 36px;
                height: 36px;
                background: ${color}22;
                border: 2px solid ${color};
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                color: rgba(255,255,255,0.92);
                font-size: 10px;
                font-weight: 800;
                letter-spacing: 0;
                box-shadow: 0 18px 42px rgba(0,0,0,0.75);
            ">${escapeHtml(label)}</div>`,
            className: 'custom-marker',
            iconSize: [36, 36],
            iconAnchor: [18, 18]
        });

        const marker = L.marker([Number(lat), Number(lng)], { icon: htmlIcon });
        if (options.popup) {
            marker.bindPopup(options.popup, {
                maxWidth: 300,
                className: 'map-popup'
            });
        }

        if (options.data) {
            marker.incidentData = options.data;
        }

        return marker;
    }

    function addMarker(containerId, lat, lng, options = {}) {
        const map = maps[containerId];
        if (!map) return null;

        const marker = createMarker(lat, lng, options);
        if (!marker) return null;

        marker.addTo(map);
        markers[containerId] = markers[containerId] || [];
        markers[containerId].push(marker);
        return marker;
    }

    function clearMarkers(containerId) {
        const map = maps[containerId];
        if (!map || !markers[containerId]) return;

        markers[containerId].forEach(marker => map.removeLayer(marker));
        markers[containerId] = [];
    }

    function addIncidentMarkers(containerId, incidents) {
        clearMarkers(containerId);
        if (!Array.isArray(incidents) || incidents.length === 0) return;

        incidents.map(normalizeIncident)
            .filter(i => Number.isFinite(i.lat) && Number.isFinite(i.lng))
            .forEach(incident => {
                addMarker(containerId, incident.lat, incident.lng, {
                    color: getSeverityColor(incident.severity),
                    icon: getStatusIcon(incident.status),
                    popup: createIncidentPopup(incident),
                    data: incident
                });
            });
    }

    function getSeverityColor(severity) {
        const key = typeof severity === 'string' ? severity.toLowerCase() : severity;
        const colors = {
            0: '#00FF88',
            1: '#3B82F6',
            2: '#FF9500',
            3: '#FF3B5C',
            low: '#00FF88',
            medium: '#3B82F6',
            high: '#FF9500',
            critical: '#FF3B5C'
        };
        return colors[key] || '#00FF88';
    }

    function getStatusIcon(status) {
        const key = typeof status === 'string' ? status.toLowerCase() : status;
        const icons = {
            0: 'P',
            1: 'A',
            2: 'IP',
            3: 'R',
            4: 'C',
            pending: 'P',
            assigned: 'A',
            inprogress: 'IP',
            resolved: 'R',
            closed: 'C'
        };
        return icons[key] || '';
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';
        const div = document.createElement('div');
        div.textContent = String(value);
        return div.innerHTML;
    }

    function createIncidentPopup(incident) {
        const statusNames = ['Pending', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
        const severityNames = ['Low', 'Medium', 'High', 'Critical'];
        const reportedDate = incident.reportedAt ? new Date(incident.reportedAt).toLocaleString() : '';
        const severity = typeof incident.severity === 'number'
            ? severityNames[incident.severity]
            : String(incident.severity || 'Medium');
        const status = typeof incident.status === 'number'
            ? statusNames[incident.status]
            : String(incident.status || 'Pending');

        return `
            <div style="min-width: 210px;">
                <h4 style="margin: 0 0 8px 0; color: #fff; font-weight: 800;">${escapeHtml(incident.title)}</h4>
                <p style="margin: 0 0 8px 0; font-size: 12px; color: rgba(255,255,255,0.68);">
                    ${escapeHtml(incident.categoryName)}
                </p>
                <div style="display: flex; gap: 8px; margin-bottom: 8px; flex-wrap: wrap;">
                    <span style="display: inline-block; padding: 3px 9px; border-radius: 999px; font-size: 10px; font-weight: 700; background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.9); border: 1px solid rgba(255,255,255,0.12);">
                        ${escapeHtml(severity)}
                    </span>
                    <span style="display: inline-block; padding: 3px 9px; border-radius: 999px; font-size: 10px; font-weight: 700; background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.9); border: 1px solid rgba(255,255,255,0.12);">
                        ${escapeHtml(status)}
                    </span>
                </div>
                <p style="margin: 0; font-size: 11px; color: rgba(255,255,255,0.5);">${escapeHtml(reportedDate)}</p>
            </div>
        `;
    }

    function panTo(containerId, lat, lng, zoom) {
        const map = maps[containerId];
        if (!map) return;

        if (zoom != null) {
            map.setView([lat, lng], zoom, { animate: true });
        } else {
            map.panTo([lat, lng], { animate: true });
        }
    }

    function addClusteredIncidentMarkers(containerId, incidents) {
        clearMarkerCluster(containerId);
        if (!Array.isArray(incidents) || incidents.length === 0) return;

        const map = maps[containerId];
        const L = window.L;
        if (!map || !L || !L.markerClusterGroup) return;

        const clusterGroup = L.markerClusterGroup({
            maxClusterRadius: 50,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false,
            zoomToBoundsOnClick: true,
            iconCreateFunction: function (cluster) {
                var count = cluster.getChildCount();
                var sizeClass = count < 10 ? 'small' : count < 50 ? 'medium' : 'large';
                var size = count < 10 ? 36 : count < 50 ? 44 : 52;
                return L.divIcon({
                    html: '<div style="width:' + size + 'px;height:' + size + 'px;background:rgba(0,255,136,0.22);border:2px solid #00FF88;border-radius:50%;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:800;font-size:' + (count < 10 ? 12 : 14) + 'px;box-shadow:0 0 20px rgba(0,255,136,0.3);">' + count + '</div>',
                    className: 'marker-cluster marker-cluster-' + sizeClass,
                    iconSize: [size, size],
                    iconAnchor: [size / 2, size / 2]
                });
            }
        });

        incidents.map(normalizeIncident)
            .filter(i => Number.isFinite(i.lat) && Number.isFinite(i.lng))
            .forEach(incident => {
                var marker = createMarker(incident.lat, incident.lng, {
                    color: getSeverityColor(incident.severity),
                    icon: getStatusIcon(incident.status),
                    popup: createIncidentPopup(incident),
                    data: incident
                });
                if (marker) clusterGroup.addLayer(marker);
            });

        map.addLayer(clusterGroup);
        markerClusters[containerId] = clusterGroup;
    }

    function clearMarkerCluster(containerId) {
        var map = maps[containerId];
        if (map && markerClusters[containerId]) {
            map.removeLayer(markerClusters[containerId]);
            delete markerClusters[containerId];
        }
    }
        const map = maps[containerId];
        const L = window.L;
        if (!map || !L || !Array.isArray(incidents) || incidents.length === 0) return;

        const latLngs = incidents.map(normalizeIncident)
            .filter(i => Number.isFinite(i.lat) && Number.isFinite(i.lng))
            .map(i => [i.lat, i.lng]);

        if (latLngs.length === 0) return;
        map.fitBounds(L.latLngBounds(latLngs), { padding: [50, 50], maxZoom: 15 });
    }

    function enableLocationPicker(containerId, onLocationSelected) {
        const map = maps[containerId];
        const L = window.L;
        if (!map || !L) return () => {};

        let pickerMarker = null;
        const handler = function (e) {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;

            if (pickerMarker) {
                map.removeLayer(pickerMarker);
            }

            pickerMarker = createMarker(lat, lng, { color: '#00FF88', icon: 'SET' });
            if (pickerMarker) {
                pickerMarker.addTo(map);
            }

            if (onLocationSelected) {
                onLocationSelected({ lat, lng });
            }
        };

        map.on('click', handler);
        return () => {
            map.off('click', handler);
            if (pickerMarker) map.removeLayer(pickerMarker);
        };
    }

    function normalizeHeatPoint(point) {
        return [
            Number(point.lat ?? point.Lat ?? point.latitude ?? point.Latitude),
            Number(point.lng ?? point.Lng ?? point.longitude ?? point.Longitude),
            Number(point.intensity ?? point.Intensity ?? 0.5)
        ];
    }

    function addHeatmap(containerId, points, options = {}) {
        const map = maps[containerId];
        const L = window.L;
        if (!map) {
            console.warn(`[Heatmap] Map "${containerId}" not found.`);
            return null;
        }
        if (!L || !L.heatLayer) {
            console.warn('[Heatmap] Leaflet.heat is not loaded. Ensure leaflet.heat script is loaded before map.js.');
            return null;
        }

        removeHeatmap(containerId);
        if (!Array.isArray(points) || points.length === 0) {
            console.warn('[Heatmap] No points provided.');
            return null;
        }

        const heatData = points.map(normalizeHeatPoint)
            .filter(p => Number.isFinite(p[0]) && Number.isFinite(p[1]) && Number.isFinite(p[2]));

        if (heatData.length === 0) {
            console.warn('[Heatmap] All points were invalid after normalization.');
            return null;
        }

        console.log(`[Heatmap] Adding ${heatData.length} points to "${containerId}".`);
        console.log('[Heatmap] Sample points (first 3):', heatData.slice(0, 3));
        console.log('[Heatmap] Intensity range:', Math.min(...heatData.map(p => p[2])), 'to', Math.max(...heatData.map(p => p[2])));

        const heatLayer = L.heatLayer(heatData, {
            radius: options.radius || 28,
            blur: options.blur || 18,
            maxZoom: options.maxZoom || 15,
            gradient: options.gradient || {
                0.25: '#3B82F6',
                0.55: '#FF9500',
                0.9: '#FF3B5C'
            }
        }).addTo(map);

        heatmapLayers[containerId] = heatLayer;
        return heatLayer;
    }

    function removeHeatmap(containerId) {
        const map = maps[containerId];
        if (!map || !heatmapLayers[containerId]) return;

        map.removeLayer(heatmapLayers[containerId]);
        delete heatmapLayers[containerId];
    }

    function invalidate(containerId) {
        if (maps[containerId]) {
            maps[containerId].invalidateSize();
        }
    }

    function dispose(containerId) {
        removeHeatmap(containerId);
        clearMarkers(containerId);
        if (maps[containerId]) {
            maps[containerId].remove();
            delete maps[containerId];
        }
        delete markers[containerId];
    }

    window.safezoneMap = {
        init: initMap,
        get: getMap,
        addMarker,
        clearMarkers,
        addIncidentMarkers,
        addClusteredIncidentMarkers,
        clearMarkerCluster,
        panTo,
        fitBounds,
        enableLocationPicker,
        addHeatmap,
        removeHeatmap,
        invalidate,
        dispose,
        getSeverityColor,
        DEFAULT_CENTER,
        DEFAULT_ZOOM
    };
})(window);
