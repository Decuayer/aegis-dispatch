// wwwroot/js/leafletMap.js
window.leafletMap = (function () {
    let map = null;
    let incidentClusterGroup = null;
    let teamLayerGroup = null;
    let incidentMarkers = {}; // { [incidentId]: marker }
    let teamMarkers = {};     // { [teamId]: marker }

    // Picker Mini-Map instances
    let pickerMap = null;
    let pickerMarker = null;
    let _pickerDotNetRef = null;
    let pickerTileLayer = null;

    // Get the color from EmergencyCode according to the Severity color
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

    // Color according to team status
    function getTeamColor(status) {
        switch (status) {
            case 'Idle':      return '#22c55e';   // yeşil
            case 'Forwarded': return '#3b82f6';   // mavi
            case 'OnScene':   return '#f59e0b';   // sarı
            case 'Busy':      return '#ef4444';   // kırmızı
            default:          return '#6b7280';
        }
    }

    // Custom SVG-based incident marker icon
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

    // SVG-based custom team marker icon
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

    // Initialize the map
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

        // Cluster group — for incidents
        incidentClusterGroup = L.markerClusterGroup({
            maxClusterRadius: 60,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false
        });
        map.addLayer(incidentClusterGroup);

        // Normal layer group — for teams
        teamLayerGroup = L.layerGroup().addTo(map);

        // Layer Control
        const overlays = {
            "🔴 Active Incidents": incidentClusterGroup,
            "🟢 Field Teams": teamLayerGroup
        };
        L.control.layers(null, overlays, { position: 'topright', collapsed: false }).addTo(map);
    }

    // Helper: Incident Popup HTML Template Generator
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

    // Add incident marker
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

    // Add / update team marker (Restored Function)
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
    // Smooth Team Marker Animation (GPS Transition)
    // ──────────────────────────────────────────────
    function animateTeamMarker(teamId, targetLat, targetLng) {
        const marker = teamMarkers[teamId];
        if (!marker) return;

        if (marker._animFrameId) {
            cancelAnimationFrame(marker._animFrameId);
        }

        const start = marker.getLatLng();
        const duration = 1000;
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

    // Remove marker
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

    // Incident Status Update
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

    // Team Status Update
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

    // Incident Assigned Team Update
    function updateIncidentAssignment(incidentId, teamId, teamName) {
        const marker = incidentMarkers[incidentId];
        if (!marker) return;

        if (marker._incidentData) {
            marker._incidentData.assignedTeamName = teamName;
            marker.setPopupContent(createIncidentPopupHtml(marker._incidentData));
        }
    }

    // Pan & Zoom
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

    // Callback hooks for Blazor (DotNet reference)
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

    // Clear/destroy map
    function destroyMap() {
        if (map) {
            map.remove();
            map = null;
            incidentMarkers = {};
            teamMarkers = {};
        }
    }

    // Interactive Mini-Map Coordinate Picker
    function initPickerMap(containerId, initialLat, initialLng, initialZoom, dotNetRef, tileProvider) {
        if (pickerMap) {
            pickerMap.remove();
            pickerMap = null;
        }

        _pickerDotNetRef = dotNetRef;
        const lat = initialLat || 40.409264;
        const lng = initialLng || 49.867092;
        const zoom = initialZoom || 14;

        pickerMap = L.map(containerId, {
            center: [lat, lng],
            zoom: zoom,
            zoomControl: true,
            attributionControl: true
        });

        // Tile layer provider selection
        const tileUrl = getTileUrl(tileProvider);
        pickerTileLayer = L.tileLayer(tileUrl, {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(pickerMap);

        // Draggable location pin
        const pickerIcon = L.divIcon({
            html: `
              <svg xmlns="http://www.w3.org/2000/svg" width="34" height="42" viewBox="0 0 34 42">
                <path d="M17 0C7.6 0 0 7.6 0 17c0 12.8 17 25 17 25S34 29.8 34 17C34 7.6 26.4 0 17 0z" fill="#3b82f6" stroke="#ffffff" stroke-width="2"/>
                <circle cx="17" cy="17" r="6" fill="#ffffff"/>
              </svg>`,
            className: '',
            iconSize: [34, 42],
            iconAnchor: [17, 42]
        });

        pickerMarker = L.marker([lat, lng], {
            draggable: true,
            icon: pickerIcon
        }).addTo(pickerMap);

        // Click event on map to reposition marker
        pickerMap.on('click', function (e) {
            const clickedLat = e.latlng.lat;
            const clickedLng = e.latlng.lng;
            pickerMarker.setLatLng([clickedLat, clickedLng]);
            notifyPickerLocation(clickedLat, clickedLng);
        });

        // Dragend event on marker
        pickerMarker.on('dragend', function () {
            const position = pickerMarker.getLatLng();
            notifyPickerLocation(position.lat, position.lng);
        });

        // Force map resize recalculation after modal/tab transition
        setTimeout(() => {
            if (pickerMap) pickerMap.invalidateSize();
        }, 200);
    }

    function notifyPickerLocation(lat, lng) {
        if (_pickerDotNetRef) {
            _pickerDotNetRef.invokeMethodAsync('NotifyLocationPicked', lat, lng);
        }
    }

    function setPickerLocation(lat, lng, zoom) {
        if (!pickerMap || !pickerMarker) return;
        pickerMarker.setLatLng([lat, lng]);
        pickerMap.setView([lat, lng], zoom || pickerMap.getZoom());
    }

    function updatePickerTileLayer(tileProvider) {
        if (!pickerMap || !pickerTileLayer) return;
        pickerMap.removeLayer(pickerTileLayer);
        pickerTileLayer = L.tileLayer(getTileUrl(tileProvider), { maxZoom: 19 }).addTo(pickerMap);
    }

    function getTileUrl(provider) {
        switch (provider) {
            case 'CartoDark':
                return 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png';
            case 'CartoPositron':
                return 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';
            default:
                return 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
        }
    }

    function destroyPickerMap() {
        if (pickerMap) {
            pickerMap.remove();
            pickerMap = null;
            pickerMarker = null;
            _pickerDotNetRef = null;
            pickerTileLayer = null;
        }
    }

    // Browser Geolocation API Bridge
    function getCurrentBrowserLocation() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error("Geolocation is not supported by this browser."));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                (error) => {
                    let message = "Unable to retrieve your location.";
                    if (error.code === error.PERMISSION_DENIED) {
                        message = "Location permission denied by user.";
                    } else if (error.code === error.POSITION_UNAVAILABLE) {
                        message = "Location information is unavailable.";
                    } else if (error.code === error.TIMEOUT) {
                        message = "The request to get user location timed out.";
                    }
                    reject(new Error(message));
                },
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        });
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
        destroyMap,
        initPickerMap,
        setPickerLocation,
        updatePickerTileLayer,
        destroyPickerMap,
        getCurrentBrowserLocation
    };
})();
