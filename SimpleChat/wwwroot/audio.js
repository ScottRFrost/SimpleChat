
let localStream;
let peerConnections = {}; // Map of connectionId -> RTCPeerConnection
let dotNetHelper;

const config = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' }
    ]
};

window.webRtcAudio = {
    init: async (helper) => {
        dotNetHelper = helper;
    },

    startLocalStream: async () => {
        try {
            localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
            return localStream != null;
        } catch (e) {
            console.error("Error getting user media", e);
            return false;
        }
    },

    stopLocalStream: () => {
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }
        // Close all peer connections
        for (let id in peerConnections) {
            peerConnections[id].close();
            const el = document.getElementById('audio_' + id);
            if (el) el.remove();
        }
        peerConnections = {};
    },

    setMute: (isMuted) => {
        if (localStream) {
            localStream.getAudioTracks().forEach(track => {
                track.enabled = !isMuted;
            });
        }
    },

    // Initiate a connection to a specific remote peer
    initiateOffer: async (remoteConnectionId) => {
        const pc = getOrCreatePeerConnection(remoteConnectionId);
        
        // Add local tracks
        if (localStream) {
            localStream.getTracks().forEach(track => pc.addTrack(track, localStream));
        }
        
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        
        return JSON.stringify({ sdp: pc.localDescription });
    },

    processSignal: async (remoteConnectionId, signalStr) => {
        const signal = JSON.parse(signalStr);
        let pc = getOrCreatePeerConnection(remoteConnectionId);

        if (signal.sdp) {
            await pc.setRemoteDescription(new RTCSessionDescription(signal.sdp));
            
            if (signal.sdp.type === 'offer') {
                if (localStream) {
                    localStream.getTracks().forEach(track => pc.addTrack(track, localStream));
                }
                const answer = await pc.createAnswer();
                await pc.setLocalDescription(answer);
                await dotNetHelper.invokeMethodAsync('SendSignal', remoteConnectionId, JSON.stringify({ sdp: pc.localDescription }));
            }
        } else if (signal.ice) {
            await pc.addIceCandidate(new RTCIceCandidate(signal.ice));
        }
    },

    setVolume: (value) => {
        const audios = document.querySelectorAll('audio.remote-audio');
        audios.forEach(a => a.volume = value);
    }
};

function updatePeerCount() {
    if (!dotNetHelper) return;
    const count = Object.values(peerConnections).filter(pc => pc.connectionState === 'connected').length;
    dotNetHelper.invokeMethodAsync('UpdatePeerCount', count);
}

function getOrCreatePeerConnection(remoteConnectionId) {
    if (peerConnections[remoteConnectionId]) {
        return peerConnections[remoteConnectionId];
    }

    const pc = new RTCPeerConnection(config);
    peerConnections[remoteConnectionId] = pc;

    pc.onicecandidate = event => {
        if (event.candidate) {
            dotNetHelper.invokeMethodAsync('SendSignal', remoteConnectionId, JSON.stringify({ ice: event.candidate }));
        }
    };

    pc.ontrack = event => {
        console.log("Received remote track from " + remoteConnectionId);
        let audioId = 'audio_' + remoteConnectionId;
        let audioEl = document.getElementById(audioId);
        if (!audioEl) {
            audioEl = document.createElement('audio');
            audioEl.id = audioId;
            audioEl.className = 'remote-audio';
            audioEl.autoplay = true;
            audioEl.style.display = 'none';
            document.body.appendChild(audioEl);
        }
        audioEl.srcObject = event.streams[0];
    };

    pc.onconnectionstatechange = () => {
        console.log(`Connection state with ${remoteConnectionId}: ${pc.connectionState}`);
        if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed' || pc.connectionState === 'closed') {
            const audioEl = document.getElementById('audio_' + remoteConnectionId);
            if (audioEl) audioEl.remove();
            delete peerConnections[remoteConnectionId];
        }
        updatePeerCount();
    };

    return pc;
}

window.scrollToBottom = (id) => {
    const el = document.getElementById(id);
    if (el) {
        setTimeout(() => {
            el.scrollTop = el.scrollHeight;
        }, 50);
    }
};
