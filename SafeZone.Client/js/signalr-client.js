// signalr-client.js - SafeZone SignalR Connection Helper
(function(window) {
    'use strict';

    const connections = {};

    function createConnection(hubName, options = {}) {
        if (!window.signalR && !window.signalr) {
            console.warn('SignalR library not loaded. Include: https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js');
            return null;
        }

        const signalR = window.signalR || window.signalr;
        const API_BASE = window.safezoneApi?.baseUrl || 'http://localhost:5000';
        
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE}/hubs/${hubName}`, {
                accessTokenFactory: () => {
                    return window.safezoneAuth?.getToken() || '';
                },
                ...options
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connections[hubName] = connection;
        return connection;
    }

    async function startConnection(hubName) {
        const connection = connections[hubName];
        if (!connection) {
            throw new Error(`No connection for hub: ${hubName}`);
        }

        try {
            await connection.start();
            console.log(`SignalR connected to /hubs/${hubName}`);
            return true;
        } catch (err) {
            console.error(`SignalR connection failed for ${hubName}:`, err);
            return false;
        }
    }

    async function stopConnection(hubName) {
        const connection = connections[hubName];
        if (connection) {
            try {
                await connection.stop();
            } catch (err) {
                console.error(`SignalR stop failed for ${hubName}:`, err);
            }
        }
    }

    const signalrClient = {
        incidents: null,
        alerts: null,
        map: null,

        getIncidentHub: function() {
            if (!this.incidents) {
                this.incidents = createConnection('incidents');
            }
            return this.incidents;
        },

        getAlertHub: function() {
            if (!this.alerts) {
                this.alerts = createConnection('alerts');
            }
            return this.alerts;
        },

        getMapHub: function() {
            if (!this.map) {
                this.map = createConnection('map');
            }
            return this.map;
        },

        startAll: async function() {
            const results = [];
            if (this.incidents) results.push(await startConnection('incidents'));
            if (this.alerts) results.push(await startConnection('alerts'));
            if (this.map) results.push(await startConnection('map'));
            return results;
        },

        stopAll: async function() {
            for (const hubName of Object.keys(connections)) {
                await stopConnection(hubName);
            }
        }
    };

    window.safezoneSignalR = signalrClient;

})(window);
