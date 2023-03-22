import { transition, style, animate, trigger } from '@angular/animations';

const enterTransition = transition(':enter', [
  style({
    opacity: 0
  }),
  animate('1s ease-in', style({
    opacity: 1
  }))
]);

const leaveTransition = transition(':leave', [
  style({
    opacity: 1
  }),
  animate('3s ease-out', style({
    opacity: 0
  }))
]);

const fadeIn = trigger('fadeIn', [
  enterTransition
]);

const fadeOut = trigger('fadeOut', [
  leaveTransition
]);

export class AnimationPopup {
  fadeInEffect = fadeIn;
  fadeOutEffect = fadeOut;
  constructor() {
  }
}
