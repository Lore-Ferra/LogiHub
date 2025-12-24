declare var bootstrap: {
    Modal: {
        new (element: Element, options?: any): {
            show(): void;
            hide(): void;
        };
        getInstance(element: Element): {
            show(): void;
            hide(): void;
        } | null;
    };

    Offcanvas: {
        new (element: Element, options?: any): {
            show(): void;
            hide(): void;
        };
        getInstance(element: Element): {
            show(): void;
            hide(): void;
        } | null;
        getOrCreateInstance(
            element: Element,
            options?: any
        ): {
            show(): void;
            hide(): void;
        };
    };
};
