
window.startProofOfWork = async (difficulty) => {
    console.log("Difficulty received in JS:", difficulty, "→ target =", 1 << (32 - difficulty));
    const response = await fetch('/api/pow/challenge');
    const data = await response.json();               // { challenge: "base64string", salt: "xyz" }
    currentChallenge = data.challenge;

    const target = 1 << (32 - difficulty);
    let nonce = 0;
    while (true) {
        nonce++;
        /*
        if (nonce % 500000 === 0) {
            console.log("Still working… nonce =", nonce, " time is ", new Date().toLocaleTimeString());
        }*/
        if (nonce % 100000 === 0) {
            const percent = Math.min(99, Math.floor(nonce / 50000));
            const progressElement = document.getElementById("progress");
            if (progressElement) {
                progressElement.innerText = percent + "%";
            }
        }

        const hashBuffer = await crypto.subtle.digest('SHA-256',
            new TextEncoder().encode(currentChallenge + nonce));
        const view = new DataView(hashBuffer);
        const hashInt = view.getUint32(0);  // reads first 4 bytes as big-endian uint32

        // TODO: we should not be returning the challenge, its already on the server
        if (hashInt < target) {
            return { nonce: nonce, challenge: currentChallenge};
        }
    }
};