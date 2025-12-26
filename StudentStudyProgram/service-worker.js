// Service Worker for Push Notifications

const CACHE_NAME = 'etut-programi-v2';
const urlsToCache = [
    '/',
    '/Content/site.css',
    '/Content/melody-theme.css',
    '/Scripts/jquery-3.7.1.min.js',
    '/Scripts/bootstrap.min.js'
];

// Install Service Worker
self.addEventListener('install', event => {
    console.log('[Service Worker] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Caching app shell');
                return cache.addAll(urlsToCache);
            })
            .catch(err => {
                console.error('[Service Worker] Cache failed:', err);
            })
    );
    self.skipWaiting();
});

// Activate Service Worker
self.addEventListener('activate', event => {
    console.log('[Service Worker] Activating...');
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('[Service Worker] Removing old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

// Fetch Event - Network First Strategy
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // Skip Service Worker for:
    // 1. Non-GET requests
    // 2. Admin API calls (JSON endpoints)
    // 3. Any API/AJAX calls
    // 4. External resources
    if (event.request.method !== 'GET') {
        return;
    }

    // Bypass Admin and API endpoints - let them go directly to network
    if (url.pathname.startsWith('/Admin/') ||
        url.pathname.startsWith('/api/') ||
        url.pathname.startsWith('/StudySession/') ||
        url.pathname.startsWith('/Notification/') ||
        url.pathname.startsWith('/Account/') ||
        url.pathname.includes('Get') ||
        url.pathname.includes('Json')) {
        return;
    }

    // Only handle same-origin requests for static assets
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    event.respondWith(
        fetch(event.request)
            .then(response => {
                // Only cache successful responses for static assets
                if (response.status === 200 &&
                    (url.pathname.endsWith('.css') ||
                        url.pathname.endsWith('.js') ||
                        url.pathname.endsWith('.png') ||
                        url.pathname.endsWith('.jpg') ||
                        url.pathname.endsWith('.svg') ||
                        url.pathname === '/')) {
                    const responseClone = response.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return response;
            })
            .catch(() => {
                // Fall back to cache if network fails
                return caches.match(event.request);
            })
    );
});

// Push Event - Show Notification
self.addEventListener('push', event => {
    console.log('[Service Worker] Push received');

    let data = {
        title: 'Etüt Programı',
        body: 'Yeni bir bildiriminiz var',
        icon: '/Content/Images/icon-192.png',
        badge: '/Content/Images/badge-72.png',
        data: {
            url: '/'
        }
    };

    if (event.data) {
        try {
            const payload = event.data.json();
            data = {
                title: payload.title || data.title,
                body: payload.body || data.body,
                icon: payload.icon || data.icon,
                badge: payload.badge || data.badge,
                data: {
                    url: payload.url || '/',
                    studySessionId: payload.studySessionId
                }
            };
        } catch (e) {
            data.body = event.data.text();
        }
    }

    const options = {
        body: data.body,
        icon: data.icon,
        badge: data.badge,
        vibrate: [200, 100, 200],
        data: data.data,
        actions: [
            {
                action: 'view',
                title: 'Görüntüle'
            },
            {
                action: 'close',
                title: 'Kapat'
            }
        ],
        requireInteraction: false,
        renotify: true,
        tag: 'etut-notification'
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

// Notification Click Event
self.addEventListener('notificationclick', event => {
    console.log('[Service Worker] Notification clicked');

    event.notification.close();

    if (event.action === 'close') {
        return;
    }

    const urlToOpen = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(windowClients => {
                // Check if there's already a window open
                for (let client of windowClients) {
                    if (client.url === urlToOpen && 'focus' in client) {
                        return client.focus();
                    }
                }
                // If no window is open, open a new one
                if (clients.openWindow) {
                    return clients.openWindow(urlToOpen);
                }
            })
    );
});

// Notification Close Event
self.addEventListener('notificationclose', event => {
    console.log('[Service Worker] Notification closed', event.notification);
});

// Background Sync (for future use)
self.addEventListener('sync', event => {
    console.log('[Service Worker] Background sync', event.tag);

    if (event.tag === 'sync-notes') {
        event.waitUntil(
            // Sync notes when back online
            Promise.resolve()
        );
    }
});

// Message Event - Communication with main thread
self.addEventListener('message', event => {
    console.log('[Service Worker] Message received:', event.data);

    if (event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }

    if (event.data.type === 'CLIENTS_CLAIM') {
        self.clients.claim();
    }
});
