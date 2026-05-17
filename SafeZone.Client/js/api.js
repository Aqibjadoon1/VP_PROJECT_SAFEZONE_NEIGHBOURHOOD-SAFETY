// api.js - SafeZone API Client Wrapper
(function(window) {
    'use strict';

    const API_BASE = 'http://localhost:5000';

    async function request(endpoint, options = {}) {
        const url = `${API_BASE}${endpoint}`;
        const token = window.safezoneAuth?.getToken();

        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const config = {
            method: options.method || 'GET',
            headers,
            credentials: 'include',
            ...options
        };

        if (options.body && typeof options.body === 'object') {
            config.body = JSON.stringify(options.body);
        }

        try {
            const response = await fetch(url, config);
            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                if (response.status === 401) {
                    if (window.safezoneAuth) {
                        window.safezoneAuth.logout();
                    }
                    throw new Error('Session expired. Please login again.');
                }
                throw new Error(data.message || `Request failed: ${response.status}`);
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    const api = {
        baseUrl: API_BASE,

        auth: {
            login: (phoneNumber, password) => request('/api/auth/login', {
                method: 'POST',
                body: { phoneNumber, password }
            }),
            register: (data) => request('/api/auth/register', {
                method: 'POST',
                body: data
            }),
            logout: () => request('/api/auth/logout', {
                method: 'POST'
            }),
            me: () => request('/api/auth/me')
        },

        get: (endpoint) => request(endpoint),
        post: (endpoint, body) => request(endpoint, { method: 'POST', body }),
        put: (endpoint, body) => request(endpoint, { method: 'PUT', body }),
        delete: (endpoint) => request(endpoint, { method: 'DELETE' })
    };

    window.safezoneApi = api;

})(window);
