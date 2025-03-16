let clicks = 0; // Contador de cliques
let timeLeft = 30; // Tempo inicial em segundos
let timer; // Variável para armazenar o ID do intervalo

// Função para iniciar o timer
function startTimer() {
  timer = setInterval(() => {
    timeLeft--;
    document.getElementById("timer").textContent = timeLeft;
    if (timeLeft <= 0) {
      clearInterval(timer);
      alert("Tempo acabou!");
      saveScore(clicks);
    }
  }, 1000);
}

startTimer();
