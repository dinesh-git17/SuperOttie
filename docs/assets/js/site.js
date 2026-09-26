// Super Ottie site: small progressive enhancements. Everything works without JavaScript.
(() => {
  document.documentElement.classList.add("js");

  // Header border once the page has scrolled past the top (observer, not a scroll listener).
  const header = document.querySelector(".site-header");
  const sentinel = document.querySelector(".sentinel");
  if (header && sentinel && "IntersectionObserver" in window) {
    new IntersectionObserver(([entry]) => header.classList.toggle("is-stuck", !entry.isIntersecting)).observe(sentinel);
  }

  // Reveal sections as they enter the viewport.
  const reveals = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window) {
    const io = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        entry.target.classList.add("is-in");
        io.unobserve(entry.target);
      }
    }, { rootMargin: "0px 0px -10% 0px", threshold: 0.12 });
    reveals.forEach((el) => io.observe(el));
  } else {
    reveals.forEach((el) => el.classList.add("is-in"));
  }

  // Close the mobile menu after choosing a link.
  const menu = document.querySelector(".menu");
  menu?.querySelectorAll("a").forEach((a) => a.addEventListener("click", () => menu.removeAttribute("open")));

  // Worlds gallery: previous / next buttons that disable at either end.
  const gallery = document.querySelector(".gallery");
  const prev = document.querySelector("[data-gallery-prev]");
  const next = document.querySelector("[data-gallery-next]");
  if (gallery && prev && next) {
    const cards = gallery.querySelectorAll(".world");
    const step = () => (cards[0]?.getBoundingClientRect().width ?? 400) + 20;
    prev.addEventListener("click", () => gallery.scrollBy({ left: -step(), behavior: "smooth" }));
    next.addEventListener("click", () => gallery.scrollBy({ left: step(), behavior: "smooth" }));
    if ("IntersectionObserver" in window && cards.length) {
      const edges = new IntersectionObserver((entries) => {
        for (const entry of entries) {
          const done = entry.intersectionRatio > 0.9;
          if (entry.target === cards[0]) prev.disabled = done;
          if (entry.target === cards[cards.length - 1]) next.disabled = done;
        }
      }, { root: gallery, threshold: [0, 0.9, 1] });
      edges.observe(cards[0]);
      edges.observe(cards[cards.length - 1]);
    }
  }

  // Policy pages: highlight the section being read in the table of contents.
  const tocLinks = document.querySelectorAll(".toc a");
  if (tocLinks.length && "IntersectionObserver" in window) {
    const byId = new Map([...tocLinks].map((a) => [a.getAttribute("href").slice(1), a]));
    const spy = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        tocLinks.forEach((a) => a.classList.remove("is-active"));
        byId.get(entry.target.id)?.classList.add("is-active");
      }
    }, { rootMargin: "-20% 0px -70% 0px" });
    byId.forEach((_, id) => { const s = document.getElementById(id); if (s) spy.observe(s); });
  }
})();
