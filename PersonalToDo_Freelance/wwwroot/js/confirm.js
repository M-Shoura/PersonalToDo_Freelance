(function(){
  const modalEl = document.getElementById('confirmModal');
  if (!modalEl) return;
  const bsModal = new bootstrap.Modal(modalEl);
  const bodyEl = modalEl.querySelector('#confirmModalBody');
  const confirmBtn = modalEl.querySelector('#confirmModalConfirm');
  let targetAction = null;

  function setupForElement(el){
    const msg = el.getAttribute('data-confirm') || 'Are you sure?';
    const title = el.getAttribute('data-confirm-title');
    if(title){
      const label = modalEl.querySelector('#confirmModalLabel');
      if(label) label.textContent = title;
    }
    bodyEl.textContent = msg;
    // store element or form
    if(el.tagName === 'FORM'){
      targetAction = { type: 'form', form: el };
    } else if(el.closest('form')){
      targetAction = { type: 'form', form: el.closest('form') };
    } else if(el.tagName === 'A'){
      targetAction = { type: 'link', href: el.href };
    } else {
      targetAction = { type: 'element', el };
    }
    bsModal.show();
  }

  document.addEventListener('click', function(e){
    const el = e.target.closest('[data-confirm]');
    if(!el) return;
    e.preventDefault();
    setupForElement(el);
  });

  confirmBtn.addEventListener('click', function(){
    if(!targetAction){ bsModal.hide(); return; }
    if(targetAction.type === 'form' && targetAction.form){
      targetAction.form.submit();
    } else if(targetAction.type === 'link'){
      window.location = targetAction.href;
    } else if(targetAction.type === 'element'){
      const el = targetAction.el;
      if(el.tagName === 'BUTTON' && el.type === 'submit' && el.closest('form')){
        el.closest('form').submit();
      } else if(el.onclick){
        el.onclick();
      } else if(el.dataset && el.dataset.action){
        // trigger a click
        el.click();
      }
    }
    bsModal.hide();
  });
})();
