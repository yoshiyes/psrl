document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-filters-form]").forEach((form) => {
        const searchInput = form.querySelector("input[name='q']");
        const clearSearchButton = form.querySelector("[data-clear-search]");
        const pageInput = form.querySelector("input[name='page']");
        const sortInput = form.querySelector("input[name='sort']");
        const sortOnSearch = form.dataset.sortOnSearch === "true";

        if (searchInput && clearSearchButton) {
            const toggleClearSearchButton = () => {
                clearSearchButton.classList.toggle("hidden", searchInput.value.length === 0);
            };

            clearSearchButton.addEventListener("click", () => {
                searchInput.value = "";
                if (sortOnSearch && sortInput) {
                    sortInput.value = "newest";
                }
                searchInput.focus();
                toggleClearSearchButton();

                searchInput.dispatchEvent(new Event("input", {
                    bubbles: true
                }));
            });

            searchInput.addEventListener("input", toggleClearSearchButton);
            toggleClearSearchButton();
        }

        document.body.addEventListener("htmx:config:request", (event) => {
            const ctx = event.detail.ctx;
            if (ctx?.sourceElement !== form) {
                return;
            }

            if (!pageInput || !sortInput) {
                return;
            }

            const triggeringEvent = ctx.sourceEvent;
            const submitter = ctx.request?.submitter || triggeringEvent?.submitter;
            const target = triggeringEvent?.target;

            const isPaginationButton = submitter?.name === "page";
            const isSortButton = submitter?.name === "sort";
            const isCategoryFilter = target?.matches?.("[name='category']");
            const isSearchInput = target?.matches?.("input[name='q']");

            if (isPaginationButton) {
                pageInput.value = submitter.value;
                ctx.request.body.set("page", submitter.value);
                return;
            }

            if (isSortButton) {
                sortInput.value = submitter.value;
                ctx.request.body.set("sort", submitter.value);
            }

            if (isSearchInput && sortOnSearch) {
                const hasQuery = searchInput && searchInput.value.trim().length > 0;
                const newSort = hasQuery ? "relevance" : "newest";
                sortInput.value = newSort;
                ctx.request.body.set("sort", newSort);
            }

            if (isSortButton || isCategoryFilter || isSearchInput) {
                pageInput.value = "1";
                ctx.request.body.set("page", "1");
            }
        });
    });
});
