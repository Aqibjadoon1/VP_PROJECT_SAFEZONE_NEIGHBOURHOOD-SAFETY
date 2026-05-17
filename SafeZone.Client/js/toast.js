// toast.js - SafeZone Toast Notification System
(function(window) {
    'use strict';

    let toastContainer = null;

    function ensureContainer() {
        if (toastContainer) return;
        
        toastContainer = document.createElement('div');
        toastContainer.id = 'safezone-toasts';
        toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 10000;
            display: flex;
            flex-direction: column;
            gap: 12px;
            pointer-events: none;
        `;
        document.body.appendChild(toastContainer);
    }

    function createToast(message, type = 'info', duration = 4000) {
        ensureContainer();

        const colors = {
            success: { bg: 'rgba(0, 255, 136, 0.15)', border: 'rgba(0, 255, 136, 0.4)', icon: '&#10003;', text: '#00FF88' },
            error: { bg: 'rgba(255, 59, 92, 0.15)', border: 'rgba(255, 59, 92, 0.4)', icon: '&#10005;', text: '#FF3B5C' },
            warning: { bg: 'rgba(255, 149, 0, 0.15)', border: 'rgba(255, 149, 0, 0.4)', icon: '&#9888;', text: '#FF9500' },
            info: { bg: 'rgba(59, 130, 246, 0.15)', border: 'rgba(59, 130, 246, 0.4)', icon: '&#8505;', text: '#3B82F6' }
        };

        const color = colors[type] || colors.info;

        const toast = document.createElement('div');
        toast.style.cssText = `
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 14px 20px;
            background: ${color.bg};
            backdrop-filter: blur(16px);
            -webkit-backdrop-filter: blur(16px);
            border: 1px solid ${color.border};
            border-radius: 12px;
            min-width: 280px;
            max-width: 400px;
            pointer-events: auto;
            animation: toastEnter 0.3s ease-out;
            font-family: 'DM Sans', system-ui, sans-serif;
            font-size: 0.9rem;
            color: #fff;
        `;

        toast.innerHTML = `
            <span style="color: ${color.text}; font-size: 1.2rem; font-weight: bold;">${color.icon}</span>
            <span style="flex: 1;">${message}</span>
            <button style="
                background: none;
                border: none;
                color: rgba(255,255,255,0.5);
                cursor: pointer;
                font-size: 1.1rem;
                padding: 0;
                margin-left: 8px;
            " class="toast-close">&#10005;</button>
        `;

        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => dismissToast(toast));

        toastContainer.appendChild(toast);

        if (duration > 0) {
            setTimeout(() => dismissToast(toast), duration);
        }

        return toast;
    }

    function dismissToast(toast) {
        if (!toast || !toast.parentNode) return;
        
        toast.style.animation = 'toastExit 0.3s ease-in forwards';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }

    const toast = {
        success: (msg, duration) => createToast(msg, 'success', duration),
        error: (msg, duration) => createToast(msg, 'error', duration),
        warning: (msg, duration) => createToast(msg, 'warning', duration),
        info: (msg, duration) => createToast(msg, 'info', duration),
        custom: createToast
    };

    window.safezoneToast = toast;

    const style = document.createElement('style');
    style.textContent = `
        @keyframes toastEnter {
            from {
                opacity: 0;
                transform: translateX(100px);
            }
            to {
                opacity: 1;
                transform: translateX(0);
            }
        }
        @keyframes toastExit {
            from {
                opacity: 1;
                transform: translateX(0);
            }
            to {
                opacity: 0;
                transform: translateX(50px);
            }
        }
    `;
    document.head.appendChild(style);

})(window);
