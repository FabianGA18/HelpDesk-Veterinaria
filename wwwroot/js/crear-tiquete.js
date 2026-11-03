// Validación de evidencia en el cliente (RNF-06). El servidor debe validar también.
(() => {
  const MAX = 10 * 1024 * 1024;
  const TIPOS = ['application/pdf', 'image/jpeg', 'image/png'];
  const form = document.getElementById('form-crear');
  const input = document.getElementById('evidencias');
  const drop = document.getElementById('drop');
  const lista = document.getElementById('lista-archivos');
  const error = document.getElementById('error-form');
  let archivos = [];

  const sync = () => {
    const dt = new DataTransfer();
    archivos.forEach(f => dt.items.add(f));
    input.files = dt.files;
    lista.innerHTML = '';
    archivos.forEach((f, i) => {
      const chip = document.createElement('span');
      chip.className = 'file';
      chip.textContent = f.name;
      const x = document.createElement('button');
      x.type = 'button'; x.textContent = '×'; x.setAttribute('aria-label', 'Quitar ' + f.name);
      x.onclick = () => { archivos.splice(i, 1); sync(); };
      chip.appendChild(x);
      lista.appendChild(chip);
    });
  };

  const agregar = (fl) => {
    const rechazados = [];
    [...fl].forEach(f => {
      if (!TIPOS.includes(f.type)) rechazados.push(`«${f.name}»: solo PDF, JPG o PNG`);
      else if (f.size > MAX) rechazados.push(`«${f.name}»: supera 10 MB`);
      else if (!archivos.some(a => a.name === f.name)) archivos.push(f);
    });
    error.textContent = rechazados.length ? 'Archivo rechazado — ' + rechazados.join('; ') : '';
    sync();
  };

  drop.addEventListener('click', () => input.click());
  drop.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); input.click(); } });
  input.addEventListener('change', () => { const nuevos = [...input.files]; archivos = archivos.filter(a => !nuevos.some(n => n.name === a.name)); agregar(nuevos); });
  ['dragover', 'dragleave', 'drop'].forEach(ev => drop.addEventListener(ev, e => {
    e.preventDefault(); drop.classList.toggle('over', ev === 'dragover');
    if (ev === 'drop') agregar(e.dataTransfer.files);
  }));

  form.addEventListener('submit', e => {
    const ok = form.Asunto.value.trim() && form.CategoriaId.value && form.Descripcion.value.trim();
    if (!ok) { e.preventDefault(); error.textContent = 'Completa asunto, categoría y descripción.'; }
  });
})();
