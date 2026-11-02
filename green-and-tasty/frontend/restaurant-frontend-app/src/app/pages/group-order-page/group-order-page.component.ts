import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Subject, Subscription, forkJoin, merge, timer } from 'rxjs';
import { catchError, exhaustMap, filter, switchMap } from 'rxjs/operators';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

@Component({
  selector: 'app-group-order-page',
  imports: [CurrencyPipe, DatePipe, ParticipantCardComponent, ModalComponent],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);

  private pollingSubscription: Subscription | null = null;
  private readonly refreshTrigger$ = new Subject<void>();

  readonly loading = signal(true);
  readonly error = signal<'not_found' | 'general' | null>(null);

  readonly invite = signal<GuestInvite | null>(null);
  readonly ownerSessionId = signal<string | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly groupStatus = signal<GroupReservationStatus>('Open');

  readonly isSubmitModalOpen = signal(false);
  readonly isSubmitting = signal(false);

  readonly guestToRemove = signal<Participant | null>(null);
  readonly isRemoving = signal(false);

  readonly lastUpdatedAt = signal<Date | null>(null);
  readonly syncError = signal<string | null>(null);

  readonly reservationId = computed(() => {
    return this.route.snapshot.paramMap.get('reservationId') ?? '';
  });

  readonly statusBadgeText = computed(() => {
    return this.groupStatus() === 'Submitted' ? 'Sent to Restaurant' : 'Collecting pre-orders';
  });

  readonly guestCountText = computed(() => {
    const max = this.invite()?.MaxGuestCount ?? 0;
    return `${this.participants().length} of ${max} guests`;
  });

  readonly consolidatedOrder = computed(() => {
    return consolidatePreOrder(this.participants());
  });

  readonly grandTotal = computed(() => {
    return this.consolidatedOrder().total;
  });

  readonly hasAnyDishes = computed(() => {
    return this.participants().some((person) => person.items.length > 0);
  });

  readonly unsubmittedCount = computed(() => {
    return this.participants().filter((person) => !person.isSubmitted).length;
  });

  readonly unsubmittedNotice = computed(() => {
    const count = this.unsubmittedCount();
    if (count === 0) return null;
    const noun = count === 1 ? '1 participant hasn’t' : `${count} participants haven’t`;
    return `${noun} submitted their cart yet — their dishes won't be included in the order`;
  });

  readonly canSubmit = computed(() => {
    return (
      this.consolidatedOrder().items.length > 0 &&
      !this.isSubmitting() &&
      this.groupStatus() !== 'Submitted'
    );
  });

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadData(): void {
    this.stopPolling();
    this.loading.set(true);
    this.error.set(null);

    const resId = this.reservationId();
    if (!resId) {
      this.error.set('not_found');
      this.loading.set(false);
      return;
    }

    this.groupOrdersService
      .getGroupInvite$(resId)
      .pipe(
        switchMap((invite) => {
          this.invite.set(invite);
          return forkJoin({
            sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId),
            ownerSessionId: this.groupOrdersService.getOwnerSessionId$(resId),
            status: this.groupOrdersService.getStatus$(invite.GroupReservationId),
          });
        })
      )
      .subscribe({
        next: ({ sessions, ownerSessionId, status }) => {
          this.participants.set(sessions.participants);
          this.ownerSessionId.set(ownerSessionId);
          this.groupStatus.set(status.status);
          this.lastUpdatedAt.set(new Date());
          this.loading.set(false);
          this.startPolling();
        },
        error: (err: unknown) => {
          this.loading.set(false);
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.error.set('not_found');
          } else {
            this.error.set('general');
          }
        },
      });
  }

  startPolling(): void {
    this.stopPolling();
    if (this.groupStatus() !== 'Open') {
      return;
    }

    const timer$ = timer(15000, 15000);
    const poll$ = merge(timer$, this.refreshTrigger$);

    this.pollingSubscription = poll$
      .pipe(
        filter(() => this.groupStatus() === 'Open'),
        exhaustMap(() => {
          const currentInvite = this.invite();
          if (!currentInvite) {
            return EMPTY;
          }
          return this.groupOrdersService
            .getSessions$(currentInvite.GroupReservationId, { silent: true })
            .pipe(
              catchError(() => {
                this.syncError.set('Couldn’t refresh');
                return EMPTY;
              })
            );
        })
      )
      .subscribe({
        next: (sessions) => {
          this.participants.set(sessions.participants);
          this.lastUpdatedAt.set(new Date());
          this.syncError.set(null);
        },
      });
  }

  stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  refreshNow(): void {
    const currentInvite = this.invite();
    if (!currentInvite || this.groupStatus() !== 'Open') {
      return;
    }
    this.refreshTrigger$.next();
  }

  openSubmitModal(): void {
    this.isSubmitModalOpen.set(true);
  }

  closeSubmitModal(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.isSubmitModalOpen.set(false);
  }

  confirmSubmit(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.isSubmitting.set(true);

    const currentInvite = this.invite();
    const resId = this.reservationId();
    if (!currentInvite || !resId) {
      this.isSubmitting.set(false);
      return;
    }

    this.groupOrdersService.submit$(currentInvite.GroupReservationId, resId).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isSubmitModalOpen.set(false);
        this.groupStatus.set('Submitted');
        this.toastService.success('Pre-order sent to the restaurant');
        this.stopPolling();
      },
      error: () => {
        this.isSubmitting.set(false);
        this.isSubmitModalOpen.set(false);
      },
    });
  }

  openRemoveGuestModal(guest: Participant): void {
    this.guestToRemove.set(guest);
  }

  closeRemoveGuestModal(): void {
    if (this.isRemoving()) {
      return;
    }
    this.guestToRemove.set(null);
  }

  confirmRemoveGuest(): void {
    const guest = this.guestToRemove();
    const currentInvite = this.invite();
    if (!guest || !currentInvite || this.isRemoving()) {
      return;
    }

    this.isRemoving.set(true);
    this.groupOrdersService
      .removeSession$(currentInvite.GroupReservationId, guest.id)
      .subscribe({
        next: (response) => {
          this.isRemoving.set(false);
          this.participants.set(response.participants);
          this.closeRemoveGuestModal();
        },
        error: () => {
          this.isRemoving.set(false);
          this.closeRemoveGuestModal();
        },
      });
  }
}
