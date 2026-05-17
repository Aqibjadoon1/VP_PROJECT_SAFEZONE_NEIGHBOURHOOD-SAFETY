// auth.js - SafeZone Authentication State Management
(function(window) {
    'use strict';

    const TOKEN_KEY = 'safezone_token';
    const REFRESH_TOKEN_KEY = 'safezone_refresh_token';
    const USER_KEY = 'safezone_user';
    const EXPIRY_KEY = 'safezone_expiry';

    const auth = {
        login: function(token, refreshToken, expiresAt, user) {
            localStorage.setItem(TOKEN_KEY, token);
            localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
            localStorage.setItem(EXPIRY_KEY, expiresAt);
            localStorage.setItem(USER_KEY, JSON.stringify(user));

            window.dispatchEvent(new CustomEvent('safezone:auth-change', {
                detail: { isAuthenticated: true, user }
            }));

            return true;
        },

        logout: function() {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(REFRESH_TOKEN_KEY);
            localStorage.removeItem(EXPIRY_KEY);
            localStorage.removeItem(USER_KEY);

            window.dispatchEvent(new CustomEvent('safezone:auth-change', {
                detail: { isAuthenticated: false, user: null }
            }));

            if (!window.location.pathname.includes('login.html') && 
                !window.location.pathname.includes('register.html') &&
                window.location.pathname !== '/' &&
                window.location.pathname !== '/index.html') {
                window.location.href = '../login.html';
            }
        },

        getToken: function() {
            const expiry = localStorage.getItem(EXPIRY_KEY);
            if (expiry) {
                const expiryDate = new Date(expiry);
                if (expiryDate <= new Date()) {
                    this.logout();
                    return null;
                }
            }
            return localStorage.getItem(TOKEN_KEY);
        },

        getRefreshToken: function() {
            return localStorage.getItem(REFRESH_TOKEN_KEY);
        },

        getUser: function() {
            const userJson = localStorage.getItem(USER_KEY);
            return userJson ? JSON.parse(userJson) : null;
        },

        isAuthenticated: function() {
            const token = this.getToken();
            return !!token;
        },

        getRole: function() {
            const user = this.getUser();
            return user?.role || null;
        },

        isResident: function() {
            return this.getRole() === 'Resident';
        },

        isAuthority: function() {
            return this.getRole() === 'Authority';
        },

        isSuperAdmin: function() {
            return this.getRole() === 'SuperAdmin';
        },

        getDashboardUrl: function() {
            const role = this.getRole();
            if (role === 'Authority' || role === 'SuperAdmin') {
                return 'authority/dashboard.html';
            }
            return 'user/dashboard.html';
        },

        requireAuth: function() {
            if (!this.isAuthenticated()) {
                let redirectTo = '../login.html';
                window.location.href = redirectTo;
                return false;
            }
            return true;
        },

        redirectIfAuthenticated: function() {
            if (this.isAuthenticated()) {
                window.location.href = this.getDashboardUrl();
                return true;
            }
            return false;
        }
    };

    window.safezoneAuth = auth;

    document.addEventListener('DOMContentLoaded', function() {
    });

})(window);
