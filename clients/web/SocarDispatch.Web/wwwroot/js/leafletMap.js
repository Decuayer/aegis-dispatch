// wwwroot/js/leafletMap.js
window.leafletMap = (function () {
    let map = null;
    let incidentClusterGroup = null;
    let teamLayerGroup = null;
    let incidentMarkers = {}; // { [incidentId]: marker }
    let teamMarkers = {};     // { [teamId]: marker }

    // ──────────────────────────────────────────────
    // Severity rengine göre EmergencyCode'dan renk al
    // ──────────────────────────────────────────────
    function getIncidentColor(emergencyCode) {
        const code = (emergencyCode || '').toLowerCase();
        if (code.includes('kirmizi') || code.includes('red') || code.includes('1'))
            return '#ef4444';
        if (code.includes('turuncu') || code.includes('orange') || code.includes('2'))
            return '#f97316';
        if (code.includes('sari') || code.includes('yellow') || code.includes('3'))
            return '#f59e0b';
        return '#3b82f6'; // varsayılan: mavi
    }

    // Team status'e göre renk
    function getTeamColor(status) {
        switch (status) {
            case 'Idle':      return '#22c55e';   // yeşil
            case 'Forwarded': return '#3b82f6';   // mavi
            case 'OnScene':   return '#f59e0b';   // sarı
            case 'Busy':      return '#ef4444';   // kırmızı
            default:          return '#6b7280';
        }
    }

    // SVG tabanlı özel incident marker ikonu
    function createIncidentIcon(emergencyCode) {
        const color = getIncidentColor(emergencyCode);
        const svg = `
          <svg xmlns="http://www.w3.org/2000/svg" width="32" height="40" viewBox="0 0 32 40">
            <path d="M16 0C7.16 0 0 7.16 0 16c0 10 16 24 16 24S32 26 32 16C32 7.16 24.84 0 16 0z"
                  fill="${color}" stroke="white" stroke-width="2"/>
            <text x="16" y="21" text-anchor="middle" font-size="14" fill="white" font-weight="bold"
                  font-family="Inter,sans-serif">!</text>
          </svg>`;
        return L.divIcon({
            html: svg,
            className: '',
            iconSize: [32, 40],
            iconAnchor: [16, 40],
            popupAnchor: [0, -40]
        });
    }

    // SVG tabanlı özel team marker ikonu
    function createTeamIcon(status) {
        const color = getTeamColor(status);
        const svg = `
          <svg xmlns="http://www.w3.org/2000/svg" width="34" height="34" viewBox="0 0 34 34">
            <circle cx="17" cy="17" r="15" fill="${color}" stroke="white" stroke-width="2"/>
            <text x="17" y="22" text-anchor="middle" font-size="14" fill="white" font-weight="bold"
                  font-family="Inter,sans-serif">T</text>
          </svg>`;
        return L.divIcon({
            html: svg,
            className: '',
            iconSize: [34, 34],
            iconAnchor: [17, 17],
            popupAnchor: [0, -17]
        });
    }

    // ──────────────────────────────────────────────
    // Haritayı başlat
    // ──────────────────────────────────────────────
    function initMap(containerId, lat, lng, zoom) {
        if (map) {
            map.remove();
            map = null;
        }

        map = L.map(containerId, {
            center: [lat, lng],
            zoom: zoom || 14,
            zoomControl: true,
            attributionControl: true
        });

        // OpenStreetMap tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        // Cluster grubu — incident'lar için
        incidentClusterGroup = L.markerClusterGroup({
            maxClusterRadius: 60,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false
        });
        map.addLayer(incidentClusterGroup);

        // Normal layer group — team'ler için
        teamLayerGroup = L.layerGroup().addTo(map);

        // Layer Control
        const overlays = {
            "🔴 Active Incidents": incidentClusterGroup,
            "🟢 Field Teams": teamLayerGroup
        };
        L.control.layers(null, overlays, { position: 'topright', collapsed: false }).addTo(map);
    }

    // ──────────────────────────────────────────────
    // Yardımcı: Incident Popup HTML Şablonu Üretici
    // ──────────────────────────────────────────────
    function createIncidentPopupHtml(data) {
        const { id, category, emergencyCode, reporterFullName, status, createdAt, assignedTeamName } = data;
        const formattedDate = new Date(createdAt).toLocaleString('tr-TR');
        const color = getIncidentColor(emergencyCode);

        const teamInfo = assignedTeamName
            ? `<div class="lf-popup-row"><span class="lf-label">Assigned Team:</span> <span class="lf-value">${assignedTeamName}</span></div>`
            : `<div class="lf-popup-row lf-unassigned">No team assigned yet</div>`;

        return `
          <div class="lf-popup">
            <div class="lf-popup-header" style="border-color: ${color}">
              <span class="lf-emergency-badge" style="background:${color}">${emergencyCode}</span>
              <strong>${category}</strong>
            </div>
            <div class="lf-popup-body">
              <div class="lf-popup-row"><span class="lf-label">Reporter:</span> <span class="lf-value">${reporterFullName}</span></div>
              <div class="lf-popup-row"><span class="lf-label">Status:</span> <span class="lf-value lf-status-${status.toLowerCase()}">${status}</span></div>
              <div class="lf-popup-row"><span class="lf-label">Time:</span> <span class="lf-value">${formattedDate}</span></div>
              ${teamInfo}
            </div>
            <div class="lf-popup-actions">
              <button class="lf-btn-primary" onclick="window.leafletMap.onIncidentDetailClick('${id}')">View Details</button>
              <button class="lf-btn-ghost" onclick="window.leafletMap.onAssignTeamClick('${id}')">Assign Team</button>
            </div>
          </div>
        `;
    }

    // ──────────────────────────────────────────────
    // Incident marker ekle
    // ──────────────────────────────────────────────
    function addIncidentMarker(incident) {
        if (!map) return;
        const { id, lat, lng, category, emergencyCode } = incident;

        if (incidentMarkers[id]) {
            incidentMarkers[id].setLatLng([lat, lng]);
            return;
        }

        const marker = L.marker([lat, lng], {
            icon: createIncidentIcon(emergencyCode),
            title: `${category} — ${emergencyCode}`
        });

        marker._incidentData = { ...incident };

        marker.bindPopup(createIncidentPopupHtml(incident), { 
            maxWidth: 280, 
            className: 'lf-custom-popup' 
        });

        incidentClusterGroup.addLayer(marker);
        incidentMarkers[id] = marker;
    }

    // ──────────────────────────────────────────────
    // Team marker ekle / güncelle (Geri Getirilen Fonksiyon)
    // ──────────────────────────────────────────────
    function addTeamMarker(team) {
        if (!map) return;
        const { id, teamName, status, lat, lng, updatedAt } = team;

        if (!lat || !lng) return;

        if (teamMarkers[id]) {
            teamMarkers[id].setLatLng([lat, lng]);
            teamMarkers[id].setIcon(createTeamIcon(status));
            teamMarkers[id].setTooltipContent(`
                <div class="lf-tooltip">
                  <strong>${teamName}</strong>
                  <span class="lf-status-badge lf-status-${status.toLowerCase()}">${status}</span>
                  <small>Updated: ${new Date(updatedAt).toLocaleTimeString('tr-TR')}</small>
                </div>
            `);
            return;
        }

        const marker = L.marker([lat, lng], {
            icon: createTeamIcon(status),
            title: teamName
        });

        marker._teamData = { ...team };

        marker.bindTooltip(`
            <div class="lf-tooltip">
              <strong>${teamName}</strong>
              <span class="lf-status-badge lf-status-${status.toLowerCase()}">${status}</span>
              <small>Updated: ${new Date(updatedAt).toLocaleTimeString('tr-TR')}</small>
            </div>
        `, { permanent: false, direction: 'top', className: 'lf-custom-tooltip' });

        teamLayerGroup.addLayer(marker);
        teamMarkers[id] = marker;
    }

    // ──────────────────────────────────────────────
    // 6.1 Smooth Team Marker Animasyonu (GPS Geçişi)
    // ──────────────────────────────────────────────
    function animateTeamMarker(teamId, targetLat, targetLng) {
        const marker = teamMarkers[teamId];
        if (!marker) return;

        if (marker._animFrameId) {
            cancelAnimationFrame(marker._animFrameId);
        }

        const start = marker.getLatLng();
        const duration = 1000; // 1 saniye geçiş süresi
        const startTime = performance.now();

        function animate(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            const easeOut = 1 - Math.pow(1 - progress, 3);

            const currentLat = start.lat + (targetLat - start.lat) * easeOut;
            const currentLng = start.lng + (targetLng - start.lng) * easeOut;

            marker.setLatLng([currentLat, currentLng]);

            if (progress < 1) {
                marker._animFrameId = requestAnimationFrame(animate);
            } else {
                marker._animFrameId = null;
            }
        }

        marker._animFrameId = requestAnimationFrame(animate);
    }

    // ──────────────────────────────────────────────
    // Marker kaldır
    // ──────────────────────────────────────────────
    function removeIncidentMarker(incidentId) {
        if (incidentMarkers[incidentId]) {
            incidentClusterGroup.removeLayer(incidentMarkers[incidentId]);
            delete incidentMarkers[incidentId];
        }
    }

    function removeTeamMarker(teamId) {
        if (teamMarkers[teamId]) {
            teamLayerGroup.removeLayer(teamMarkers[teamId]);
            delete teamMarkers[teamId];
        }
    }

    // ──────────────────────────────────────────────
    // 6.2 Incident Status Güncelleme
    // ──────────────────────────────────────────────
    function updateIncidentStatus(incidentId, newStatus) {
        const marker = incidentMarkers[incidentId];
        if (!marker) return;

        if (newStatus === 'Resolved' || newStatus === 'Canceled') {
            removeIncidentMarker(incidentId);
            return;
        }

        if (marker._incidentData) {
            marker._incidentData.status = newStatus;
            marker.setPopupContent(createIncidentPopupHtml(marker._incidentData));
        }
    }

    // ──────────────────────────────────────────────
    // 6.3 Team Status Güncelleme
    // ──────────────────────────────────────────────
    function updateTeamStatus(teamId, newStatus) {
        const marker = teamMarkers[teamId];
        if (!marker) return;

        marker.setIcon(createTeamIcon(newStatus));

        if (marker._teamData) {
            marker._teamData.status = newStatus;
            marker._teamData.updatedAt = new Date().toISOString();

            marker.setTooltipContent(`
                <div class="lf-tooltip">
                  <strong>${marker._teamData.teamName}</strong>
                  <span class="lf-status-badge lf-status-${newStatus.toLowerCase()}">${newStatus}</span>
                  <small>Updated: ${new Date().toLocaleTimeString('tr-TR')}</small>
                </div>
            `);
        }
    }

    // ──────────────────────────────────────────────
    // 6.4 Incident Atanmış Ekip Güncelleme
    // ──────────────────────────────────────────────
    function updateIncidentAssignment(incidentId, teamId, teamName) {
        const marker = incidentMarkers[incidentId];
        if (!marker) return;

        if (marker._incidentData) {
            marker._incidentData.assignedTeamName = teamName;
            marker.setPopupContent(createIncidentPopupHtml(marker._incidentData));
        }
    }

    // ──────────────────────────────────────────────
    // Pan & Zoom
    // ──────────────────────────────────────────────
    function panToIncident(incidentId) {
        if (incidentMarkers[incidentId]) {
            const latlng = incidentMarkers[incidentId].getLatLng();
            map.flyTo(latlng, 16, { animate: true, duration: 1.5 });
            incidentMarkers[incidentId].openPopup();
        }
    }

    function panToLocation(lat, lng, zoom) {
        if (map) map.flyTo([lat, lng], zoom || 15, { animate: true });
    }

    // ──────────────────────────────────────────────
    // Blazor'a geri çağırma hook'ları (DotNet referansı)
    // ──────────────────────────────────────────────
    let _dotNetRef = null;

    function setDotNetRef(dotNetRef) {
        _dotNetRef = dotNetRef;
    }

    function onIncidentDetailClick(incidentId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync('NotifyIncidentDetail', incidentId);
    }

    function onAssignTeamClick(incidentId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync('NotifyAssignTeam', incidentId);
    }

    // ──────────────────────────────────────────────
    // Haritayı temizle / yok et
    // ──────────────────────────────────────────────
    function destroyMap() {
        if (map) {
            map.remove();
            map = null;
            incidentMarkers = {};
            teamMarkers = {};
        }
    }

    // Public API
    return {
        initMap,
        addIncidentMarker,
        addTeamMarker,
        animateTeamMarker,
        updateIncidentStatus,
        updateTeamStatus,
        updateIncidentAssignment,
        removeIncidentMarker,
        removeTeamMarker,
        panToIncident,
        panToLocation,
        setDotNetRef,
        onIncidentDetailClick,
        onAssignTeamClick,
        destroyMap
    };
})();
