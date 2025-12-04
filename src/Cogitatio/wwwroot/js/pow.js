let currentChallenge = null;

window.startProofOfWork = async (difficulty) => {
    const response = await fetch('/api/pow/challenge');
    const data = await response.json();               // { challenge: "base64string", salt: "xyz" }
    currentChallenge = data.challenge;

    const target = 1 << (32 - difficulty);
    let nonce = 0;
    while (true) {
        nonce++;
        if (nonce % 100000 === 0) {
            const percent = Math.min(99, Math.floor(nonce / 50000));
            const progressElement = document.getElementById("progress");
            if (progressElement) {
                progressElement.innerText = percent + "%";
            }
        }

        const hashBuffer = await crypto.subtle.digest('SHA-256',
            new TextEncoder().encode(currentChallenge + nonce));
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashInt = (hashArray[0] << 24) | (hashArray[1] << 16) | (hashArray[2] << 8) | hashArray[3];

        if (hashInt < target) {
            return { nonce: nonce };
        }
    }
};