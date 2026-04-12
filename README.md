# Zed Zed Zed Status Cleanup

This is a small compatibility addon for **Zed Zed Zed**.

## What it changes

It keeps **Zombie Infection** as its own separate visible status.

It then folds these other zombie state hediffs into one cleaner status line in the health tab:

- Zombie rot
- Reanimating
- Fully reanimated

## Result in game

You should now see something closer to:

- Zombie Infection
- Reanimation
- Frail Biter

Or, when the pawn is actively turning:

- Zombie Infection
- Reanimation (Reanimating)

Or, after the pawn has fully turned:

- Reanimation (Reanimated)
- Frail Biter

## Fake death coma handling

If a zombie is still in the fake death turning coma and **Zombie Infection** is still present, the visible combined status will stay on:

- Reanimation (Reanimating)

That keeps the health tab cleaner without making the turn look finished too early.

## Load order

Load this **after** Zed Zed Zed.

## Notes

This addon only changes the **visible health tab presentation** for those zombie state hediffs.
It does not remove the underlying hediffs or change the actual infection or reanimation mechanics.
