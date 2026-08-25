window.socarGoogleAuth = {
    dotNetRef: null,
    clientId: null,
    isInitialized: false,

    init: function (dotNetHelper, googleClientId) {
        this.dotNetRef = dotNetHelper;
        this.clientId = googleClientId;

        if (typeof google === 'undefined' || !google.accounts || !google.accounts.id) {
            console.warn('[GoogleAuth] Google Identity Services SDK not ready yet. Retrying in 200ms...');
            setTimeout(() => this.init(dotNetHelper, googleClientId), 200);
            return;
        }

        try {
            google.accounts.id.initialize({
                client_id: this.clientId,
                callback: this.handleCredentialResponse.bind(this),
                auto_select: false,
                cancel_on_tap_outside: true,
                itp_support: true
            });

            this.renderGoogleButton();
            this.isInitialized = true;
            console.log('[GoogleAuth] Google Identity Services initialized successfully.');
        } catch (err) {
            console.error('[GoogleAuth] Initialization error:', err);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'Google Auth initialization failed: ' + err.message);
            }
        }
    },

    renderGoogleButton: function () {
        const container = document.getElementById('google-btn-container');
        if (container && typeof google !== 'undefined' && google.accounts && google.accounts.id) {
            container.innerHTML = ''; // Temizle
            google.accounts.id.renderButton(container, {
                type: 'standard',
                theme: 'filled_black', // Koyu temaya uygun
                size: 'large',
                text: 'continue_with',
                shape: 'rectangular',
                logo_alignment: 'left',
                width: container.offsetWidth || 316
            });
        }
    },

    handleCredentialResponse: function (response) {
        if (response && response.credential) {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnGoogleSignInSuccess', response.credential);
            }
        } else {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'No credential received from Google.');
            }
        }
    },

    prompt: function () {
        if (!this.isInitialized) {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'Google SDK is still loading. Please try again.');
            }
            return;
        }

        // Eğer resmi Google butonu render edildiyse doğrudan ona tıklama simülasyonu uygula
        const googleIframeBtn = document.querySelector('#google-btn-container div[role="button"]');
        if (googleIframeBtn) {
            googleIframeBtn.click();
            return;
        }

        try {
            google.accounts.id.prompt((notification) => {
                if (notification.isNotDisplayed()) {
                    console.warn('[GoogleAuth] Prompt not displayed:', notification.getNotDisplayedReason());
                    if (this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'Google sign-in prompt was blocked or closed. Please click the Google button directly.');
                    }
                } else if (notification.isSkippedMoment()) {
                    console.warn('[GoogleAuth] Prompt skipped:', notification.getSkippedReason());
                    if (this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'Google sign-in was skipped.');
                    }
                } else if (notification.isDismissedMoment()) {
                    if (notification.getDismissedReason() !== 'credential_returned' && this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', 'Google sign-in cancelled.');
                    }
                }
            });
        } catch (err) {
            console.error('[GoogleAuth] Prompt error:', err);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnGoogleSignInFailure', err.message || 'Error triggering Google prompt.');
            }
        }
    }
};
