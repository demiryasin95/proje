// Push Notifications Manager

(function () {
    'use strict';

    let vapidPublicKey = null;
    let isSubscribed = false;

    // Check if browser supports notifications
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.warn('Push notifications are not supported in this browser');
        return;
    }

    // Initialize on page load
    window.addEventListener('load', function () {
        initializePushNotifications();
    });

    async function initializePushNotifications() {
        try {
            // Register Service Worker
            const registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });

            console.log('[Push] Service Worker registered:', registration.scope);

            // Wait for service worker to be ready
            await navigator.serviceWorker.ready;

            // Get VAPID public key from server
            await loadVapidPublicKey();

            // Check current subscription status
            const subscription = await registration.pushManager.getSubscription();
            isSubscribed = subscription !== null;

            if (isSubscribed) {
                console.log('[Push] Already subscribed');
                hideNotificationBanner();
            } else {
                // Check if permission was previously denied
                if (Notification.permission === 'denied') {
                    console.warn('[Push] Notification permission denied');
                    hideNotificationBanner();
                } else if (Notification.permission === 'default') {
                    // Show banner to request permission
                    showNotificationBanner();
                }
            }

        } catch (error) {
            console.error('[Push] Initialization failed:', error);
        }
    }

    async function loadVapidPublicKey() {
        try {
            const response = await fetch('/Notification/GetVapidPublicKey');
            const data = await response.json();

            if (data.success && data.publicKey) {
                vapidPublicKey = data.publicKey;
                console.log('[Push] VAPID public key loaded');
            } else {
                console.error('[Push] Failed to load VAPID key');
            }
        } catch (error) {
            console.error('[Push] Error loading VAPID key:', error);
        }
    }

    function showNotificationBanner() {
        // Remove existing banner if any
        const existingBanner = document.getElementById('notificationBanner');
        if (existingBanner) {
            existingBanner.remove();
        }

        // Create banner
        const banner = document.createElement('div');
        banner.id = 'notificationBanner';
        banner.className = 'alert alert-info alert-dismissible fade show m-3 shadow-sm';
        banner.style.cssText = 'position: fixed; top: 70px; right: 20px; z-index: 1050; max-width: 400px; border-radius: 12px;';
        banner.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi bi-bell-fill me-3" style="font-size: 1.5rem;"></i>
                <div class="flex-grow-1">
                    <strong>Bildirimler</strong>
                    <p class="mb-2 small">Etüt hatırlatmaları almak ister misiniz?</p>
                    <button type="button" class="btn btn-sm btn-primary me-2" onclick="window.enablePushNotifications()">
                        <i class="bi bi-check-circle me-1"></i>Bildirimleri Aç
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-secondary" onclick="window.dismissNotificationBanner()">
                        Şimdi Değil
                    </button>
                </div>
                <button type="button" class="btn-close" onclick="window.dismissNotificationBanner()"></button>
            </div>
        `;

        document.body.appendChild(banner);
    }

    function hideNotificationBanner() {
        const banner = document.getElementById('notificationBanner');
        if (banner) {
            banner.remove();
        }
    }

    async function subscribeToPush() {
        try {
            const registration = await navigator.serviceWorker.ready;

            if (!vapidPublicKey) {
                throw new Error('VAPID public key not loaded');
            }

            // Subscribe to push notifications
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            });

            console.log('[Push] User subscribed:', subscription);

            // Send subscription to server
            const endpoint = subscription.endpoint;
            const keys = subscription.toJSON().keys;

            const response = await fetch('/Notification/Subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-CSRF-TOKEN': getAntiForgeryToken()
                },
                body: new URLSearchParams({
                    endpoint: endpoint,
                    p256dh: keys.p256dh,
                    auth: keys.auth
                })
            });

            const data = await response.json();

            if (data.success) {
                isSubscribed = true;
                hideNotificationBanner();
                showSuccessMessage('Bildirimler başarıyla etkinleştirildi!');
            } else {
                throw new Error(data.message || 'Subscription failed');
            }

        } catch (error) {
            console.error('[Push] Subscription failed:', error);
            showErrorMessage('Bildirimler etkinleştirilemedi: ' + error.message);
        }
    }

    async function unsubscribeFromPush() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            if (subscription) {
                await subscription.unsubscribe();

                const response = await fetch('/Notification/Unsubscribe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-CSRF-TOKEN': getAntiForgeryToken()
                    },
                    body: new URLSearchParams({
                        endpoint: subscription.endpoint
                    })
                });

                const data = await response.json();

                if (data.success) {
                    isSubscribed = false;
                    showSuccessMessage('Bildirimler kapatıldı');
                }
            }

        } catch (error) {
            console.error('[Push] Unsubscribe failed:', error);
        }
    }

    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    function showSuccessMessage(message) {
        showToast('success', 'Başarılı', message);
    }

    function showErrorMessage(message) {
        showToast('danger', 'Hata', message);
    }

    function showToast(type, title, message) {
        const bgClass = type === 'success' ? 'bg-success text-white' : 'bg-danger text-white';

        const toastHtml = `
            <div class="toast ${bgClass}" role="alert" data-bs-delay="3000">
                <div class="toast-header ${bgClass}">
                    <strong class="me-auto">${title}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">${message}</div>
            </div>
        `;

        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            document.body.appendChild(container);
        }

        container.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = container.lastElementChild;
        const toast = new bootstrap.Toast(toastElement);
        toast.show();

        setTimeout(() => {
            toastElement.remove();
        }, 3500);
    }

    // Expose functions globally
    window.enablePushNotifications = async function () {
        // Request permission
        const permission = await Notification.requestPermission();

        if (permission === 'granted') {
            await subscribeToPush();
        } else if (permission === 'denied') {
            hideNotificationBanner();
            showErrorMessage('Bildirim izni reddedildi. Tarayıcı ayarlarından izin verebilirsiniz.');
        }
    };

    window.disablePushNotifications = unsubscribeFromPush;

    window.dismissNotificationBanner = function () {
        hideNotificationBanner();
        localStorage.setItem('notificationBannerDismissed', 'true');
    };

    // Check if banner was previously dismissed
    const wasDismissed = localStorage.getItem('notificationBannerDismissed');
    if (wasDismissed === 'true' && Notification.permission === 'default') {
        // User dismissed banner before, don't show again unless they enable notifications
        hideNotificationBanner();
    }

})();
