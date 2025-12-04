async function loadDashboard() {
  const res = await fetch('/api/get_dashboard.php');
  const data = await res.json();
  document.getElementById('statActiveUsers').textContent = data.active_users || 0;
  document.getElementById('statRageQuits').textContent = data.rage_quits || 0;
  document.getElementById('statRewards').textContent = data.reward_events || 0;
  document.getElementById('statCoinNet').textContent = data.coin_net || 0;

  const ctxRooms = document.getElementById('chartRooms');
  new Chart(ctxRooms, {
    type: 'bar',
    data: {
      labels: data.room_heatmap.labels,
      datasets: [{
        label: 'Plays',
        data: data.room_heatmap.values,
        backgroundColor: ['#0d6efd','#ffc107','#dc3545']
      }]
    },
    options: { responsive: true, plugins: { legend: { display: false } } }
  });

  const ctxWL = document.getElementById('chartWinLoss');
  new Chart(ctxWL, {
    type: 'line',
    data: {
      labels: data.win_loss.labels,
      datasets: [
        { label: 'Wins', data: data.win_loss.wins, borderColor: '#0d6efd' },
        { label: 'Losses', data: data.win_loss.losses, borderColor: '#dc3545' }
      ]
    },
    options: { responsive: true }
  });

  document.getElementById('aiDecisionLog').textContent = JSON.stringify(data.recent_ai_decisions, null, 2);
}

window.addEventListener('DOMContentLoaded', loadDashboard);
