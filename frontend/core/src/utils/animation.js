
/**
 * Thin wrapper around Element.animate() that returns a Promise
 * @param el Element to animate
 * @param keyframes The keyframes to use when animating
 * @param options Either the duration of the animation or an options argument detailing how the animation should be performed
 * @returns A promise that will resolve after the animation completes or is cancelled
 */
export function animate(
    el,
    keyframes,
    options,
    ) {
    return new Promise(resolve => {
        const anim = el.animate(keyframes, options);
        anim.addEventListener("finish", () => resolve());
        anim.addEventListener("cancel", () => resolve());
    });
}
export async function onAppear(el) {
    await animate(el, [
        {opacity: 0},
        {opacity: 1}
    ], {
        duration: 200
    });
    el.style.opacity = "1";
}
export async function onExit(el, _idx, onComplete) {
    await animate(el, [
        {opacity: 1},
        {opacity: 0}
    ], {
        duration: 200
    });
    onComplete();
}
