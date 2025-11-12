// Add current date and time functionality
document.addEventListener('DOMContentLoaded', function() {
  // You can add interactive features here if needed
  
  // Example: Add a print function
  function printReceipt() {
    window.print();
  }
  
  // Example: Add click handler for printing (if you want to add a print button)
  document.addEventListener('keydown', function(e) {
    if (e.ctrlKey && e.key === 'p') {
      e.preventDefault();
      printReceipt();
    }
  });
  
  // Add some hover effects for better UX
  const infoRows = document.querySelectorAll('.info-row');
  infoRows.forEach(row => {
    row.addEventListener('mouseenter', function() {
      this.style.backgroundColor = 'rgba(255, 255, 255, 0.1)';
    });
    
    row.addEventListener('mouseleave', function() {
      this.style.backgroundColor = 'transparent';
    });
  });
  
  // Add animation for the confirmation code
  const confirmationCode = document.querySelector('.confirmation-code');
  if (confirmationCode) {
    confirmationCode.style.opacity = '0';
    confirmationCode.style.transform = 'translateY(10px)';
    
    setTimeout(() => {
      confirmationCode.style.transition = 'all 0.5s ease';
      confirmationCode.style.opacity = '1';
      confirmationCode.style.transform = 'translateY(0)';
    }, 500);
  }
});
