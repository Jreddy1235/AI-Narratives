(function(){
  // Floating avatar that talks + small inline chat input
  document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('ai-avatar')) return;

    const container = document.createElement('div');
    container.id = 'ai-avatar';
    container.innerHTML = `
      <div class="ai-avatar-orb">
        <div class="ai-avatar-face">
          <div class="ai-eye ai-eye-left"></div>
          <div class="ai-eye ai-eye-right"></div>
          <div class="ai-mouth"></div>
        </div>
      </div>
      <div class="ai-speech">
        <div id="aiBotBubble" class="ai-speech-bubble">I am your bingo coach AI. I will watch your play and suggest rooms and breaks.</div>
        <div class="ai-input-row" id="aiInputRow">
          <input type="text" id="aiBotInput" class="ai-input" placeholder="Ask for tips or a break" />
          <button class="ai-send-btn" id="aiBotSend">Send</button>
        </div>
      </div>
    `;
    document.body.appendChild(container);

    const bubble = document.getElementById('aiBotBubble');
    const avatarOrb = container.querySelector('.ai-avatar-orb');

    function setBotMessage(text) {
      bubble.textContent = text;
      talkAnimation();
      trySpeech(text);
    }

    function talkAnimation() {
      avatarOrb.classList.add('talking');
      setTimeout(() => avatarOrb.classList.remove('talking'), 1400);
    }

    function trySpeech(text) {
      if (!('speechSynthesis' in window)) return;
      const utter = new SpeechSynthesisUtterance(text);
      utter.rate = 1.05;
      utter.pitch = 1.1;
      window.speechSynthesis.cancel();
      window.speechSynthesis.speak(utter);
    }

    async function sendToAI(message) {
      if (!message) return;
      setBotMessage('Thinking about your play...');
      try {
        const userId = localStorage.getItem('user_id');
        const token = localStorage.getItem('session_token');
        
        const res = await fetch('/api/ai_decision.php', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            mode: 'chat',
            message,
            user_id: userId ? parseInt(userId) : null,
            session_token: token
          })
        });
        const data = await res.json();
        setBotMessage(data.bot_message || 'Keep going, I am tracking your streaks.');
      } catch (e) {
        setBotMessage('AI service is not available right now. Keep playing for fun!');
      }
    }

    // Toggle input focus when avatar is clicked
    avatarOrb.addEventListener('click', () => {
      const input = document.getElementById('aiBotInput');
      if (!input) return;
      input.focus();
    });

    document.getElementById('aiBotSend').addEventListener('click', () => {
      const input = document.getElementById('aiBotInput');
      const text = input.value.trim();
      if (!text) return;
      input.value = '';
      sendToAI(text);
    });

    // allow game logic to push messages into the coach bubble
    window.AICoach = window.AICoach || {};
    window.AICoach.setMessage = setBotMessage;
  });
})();

// --- 3D avatar (Three.js) ---

function loadThree(callback) {
  if (window.THREE) { callback(); return; }
  const existing = document.querySelector('script[data-threejs]');
  if (existing) {
    existing.addEventListener('load', () => callback());
    return;
  }
  const script = document.createElement('script');
  script.src = 'https://unpkg.com/three@0.160.0/build/three.min.js';
  script.async = true;
  script.dataset.threejs = 'true';
  script.onload = () => callback();
  document.head.appendChild(script);
}

function initThreeAvatar(orbEl) {
  if (!orbEl) return;

  const canvas = document.createElement('canvas');
  canvas.id = 'aiAvatarCanvas';
  orbEl.appendChild(canvas);

  loadThree(() => {
    const THREE = window.THREE;
    if (!THREE) return;

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
    camera.position.set(0, 0.4, 2.2);

    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    const size = orbEl.clientWidth || 64;
    renderer.setSize(size, size);
    renderer.setPixelRatio(window.devicePixelRatio || 1);

    const light1 = new THREE.DirectionalLight(0xffffff, 0.9);
    light1.position.set(1, 1, 1);
    scene.add(light1);
    const light2 = new THREE.AmbientLight(0xffffff, 0.5);
    scene.add(light2);

    // Toy: capsule body + head sphere (warm host palette)
    const bodyGeo = new THREE.CapsuleGeometry(0.42, 0.65, 6, 20);
    const bodyMat = new THREE.MeshStandardMaterial({ color: 0xd44b3b, metalness: 0.15, roughness: 0.45 });
    const body = new THREE.Mesh(bodyGeo, bodyMat);
    body.position.y = -0.15;
    scene.add(body);

    const headGeo = new THREE.SphereGeometry(0.40, 24, 24);
    const headMat = new THREE.MeshStandardMaterial({ color: 0xffecd9, metalness: 0.05, roughness: 0.75 });
    const head = new THREE.Mesh(headGeo, headMat);
    head.position.y = 0.6;
    scene.add(head);

    let t = 0;
    function animate() {
      requestAnimationFrame(animate);
      t += 0.01;
      const fast = orbEl.classList.contains('talking');
      const speed = fast ? 0.06 : 0.02;
      head.rotation.y += speed;
      body.rotation.y += speed;
      const bob = fast ? 0.05 : 0.03;
      head.position.y = 0.55 + Math.sin(t * 2) * bob;
      body.position.y = -0.2 + Math.sin(t * 2) * (bob / 2);
      renderer.render(scene, camera);
    }
    animate();

    // Resize with orb if needed
    window.addEventListener('resize', () => {
      const newSize = orbEl.clientWidth || 64;
      renderer.setSize(newSize, newSize);
      camera.aspect = 1;
      camera.updateProjectionMatrix();
    });
  });
}
