(function () {
  const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const motion = window.Motion;
  const deskSel = ".notion-desk__top, .notion-prop, .notion-table, .desk-mast, .desk-form, .empty-desk, .ticket, .block-page, .air-card, .onboard__panel";

  const countdown = document.querySelector("[data-countdown]");
  if (countdown) {
    const target = new Date(countdown.getAttribute("data-countdown"));
    const days = countdown.querySelector('[data-unit="days"]');
    const hours = countdown.querySelector('[data-unit="hours"]');
    const mins = countdown.querySelector('[data-unit="mins"]');
    const secs = countdown.querySelector('[data-unit="secs"]');
    const pad = function (n) { return String(Math.max(0, n)).padStart(2, "0"); };
    const tick = function () {
      const diff = Math.max(0, target.getTime() - Date.now());
      const d = Math.floor(diff / 86400000);
      const h = Math.floor((diff % 86400000) / 3600000);
      const m = Math.floor((diff % 3600000) / 60000);
      const s = Math.floor((diff % 60000) / 1000);
      if (days) days.textContent = pad(d);
      if (hours) hours.textContent = pad(h);
      if (mins) mins.textContent = pad(m);
      if (secs) secs.textContent = pad(s);
    };
    tick();
    window.setInterval(tick, 1000);
  }

  if (!reduce && motion && typeof motion.animate === "function") {
    motion.animate(deskSel, { opacity: [0, 1], y: [10, 0] }, {
      duration: 0.38,
      delay: typeof motion.stagger === "function" ? motion.stagger(0.035) : 0,
      easing: [0.22, 1, 0.36, 1]
    });
  } else if (!reduce) {
    document.querySelectorAll(deskSel).forEach(function (el, i) {
      el.animate(
        [
          { opacity: 0, transform: "translateY(10px)" },
          { opacity: 1, transform: "translateY(0)" }
        ],
        {
          duration: 320,
          delay: i * 35,
          easing: "cubic-bezier(0.22, 1, 0.36, 1)",
          fill: "forwards"
        }
      );
    });
  }

  document.querySelectorAll("form[method='post'], form[method='POST']").forEach(function (form) {
    form.addEventListener("submit", function () {
      if (form.hasAttribute("data-confirm") && form.dataset.confirmed !== "1") return;
      const btn = form.querySelector("[type=submit]");
      if (!btn || btn.dataset.busyApplied) return;
      btn.dataset.busyApplied = "1";
      btn.setAttribute("aria-busy", "true");
      btn.disabled = true;
      btn.textContent = btn.getAttribute("data-busy") || "Saving…";
    });
  });

  const dialog = document.getElementById("rd-confirm");
  const text = document.getElementById("rd-confirm-text");

  document.querySelectorAll("form[data-confirm]").forEach(function (form) {
    form.addEventListener("submit", function (e) {
      if (form.dataset.confirmed === "1") return;
      e.preventDefault();
      const message = form.getAttribute("data-confirm") || "Continue?";
      if (!dialog || typeof dialog.showModal !== "function") {
        if (window.confirm(message)) {
          form.dataset.confirmed = "1";
          form.submit();
        }
        return;
      }
      text.textContent = message;
      dialog.returnValue = "cancel";
      dialog.showModal();
      dialog.addEventListener("close", function onClose() {
        dialog.removeEventListener("close", onClose);
        if (dialog.returnValue === "ok") {
          form.dataset.confirmed = "1";
          form.submit();
        }
      });
    });
  });
})();
