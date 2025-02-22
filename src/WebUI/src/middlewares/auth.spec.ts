import { createTestingPinia } from '@pinia/testing';
import { ErrorResponse } from 'oidc-client-ts';
import Role from '@/models/role';
import { getRoute, next } from '@/__mocks__/router';

const mockedSignInSilent = vi.fn();
const mockedSignInCallback = vi.fn();
const mockedSignInSilentCallback = vi.fn();
vi.mock('@/services/auth-service', () => {
  return {
    signInSilent: mockedSignInSilent,
    userManager: {
      signinCallback: mockedSignInCallback,
      signinSilentCallback: mockedSignInSilentCallback,
    },
  };
});

import { useUserStore } from '@/stores/user';
import { authRouterMiddleware, signInCallback, signInSilentCallback } from './auth';

const userStore = useUserStore(createTestingPinia());

beforeEach(() => {
  userStore.$reset();
});

const from = getRoute();

it('skip any route validation', async () => {
  const to = getRoute({
    name: 'skip-auth-route',
    path: '/skip-auth-route',
    meta: {
      layout: 'default',
      skipAuth: true,
    },
  });

  expect(await authRouterMiddleware(to, from, next)).toEqual(true);

  expect(userStore.fetchUser).not.toBeCalled();
  expect(mockedSignInSilent).not.toBeCalled();
});

describe('route not requires any role', () => {
  const to = getRoute({
    name: 'user',
    path: '/user',
  });

  it('!user - no token -> try to signInSilent and fetch user', async () => {
    expect(await authRouterMiddleware(to, from, next)).toEqual(true);

    expect(mockedSignInSilent).toBeCalled();
    expect(userStore.fetchUser).not.toBeCalled();
  });

  it('!user - has token -> try to signInSilent and fetch user', async () => {
    mockedSignInSilent.mockResolvedValueOnce('test_token');

    expect(await authRouterMiddleware(to, from, next)).toEqual(true);

    expect(mockedSignInSilent).toBeCalled();
    expect(userStore.fetchUser).toBeCalled();
  });
});

describe('route requires role', () => {
  const to = getRoute({
    name: 'user',
    path: '/user',
    meta: {
      roles: ['User'],
    },
  });

  it('!user + no token -> go to index page', async () => {
    expect(await authRouterMiddleware(to, from, next)).toEqual({ name: 'Root' });
  });

  it('user with role:User -> validation passed', async () => {
    // userStore.user = getUser({ role: Role.User });
    userStore.$patch({ user: { role: Role.User } });

    expect(await authRouterMiddleware(to, from, next)).toEqual(true);
    expect(mockedSignInSilent).not.toBeCalled();
    expect(userStore.fetchUser).not.toBeCalled();
  });
});

describe('with Admin or Moderator role', () => {
  const to = getRoute({
    name: 'admin',
    path: '/admin',
    meta: {
      roles: ['Admin', 'Moderator'],
    },
  });

  it('user with role:User -> go to index page', async () => {
    userStore.$patch({ user: { role: Role.User } });

    expect(await authRouterMiddleware(to, from, next)).toEqual({ name: 'Root' });
  });

  it('user with role:Admin -> validation passed', async () => {
    userStore.$patch({ user: { role: Role.Admin } });

    expect(await authRouterMiddleware(to, from, next)).toEqual(true);
  });

  it('user with role:Moderator -> validation passed', async () => {
    userStore.$patch({ user: { role: Role.Moderator } });

    expect(await authRouterMiddleware(to, from, next)).toEqual(true);
  });
});

describe('signInCallback', () => {
  it('ok', async () => {
    expect(await signInCallback(getRoute(), getRoute(), next)).toEqual({ name: 'Characters' });
    expect(mockedSignInCallback).toHaveBeenCalled();
  });

  it('error - invalid grant', async () => {
    mockedSignInCallback.mockRejectedValue(
      new ErrorResponse({
        error: 'access_denied',
      })
    );

    expect(await signInCallback(getRoute(), getRoute(), next)).toEqual({ name: 'Banned' });
  });

  it('error - invalid grant', async () => {
    mockedSignInCallback.mockRejectedValue({ error: 'some error' });

    expect(await signInCallback(getRoute(), getRoute(), next)).toEqual({ name: 'Root' });
  });
});

describe('signInCallback', () => {
  it('ok', async () => {
    expect(await signInSilentCallback(getRoute(), getRoute(), next)).toStrictEqual(true);
    expect(mockedSignInSilentCallback).toHaveBeenCalled();
  });

  it('error - invalid grant', async () => {
    mockedSignInSilentCallback.mockRejectedValue(
      new ErrorResponse({
        error: 'access_denied',
      })
    );

    expect(await signInSilentCallback(getRoute(), getRoute(), next)).toEqual({ name: 'Banned' });
  });

  it('error - invalid grant', async () => {
    mockedSignInSilentCallback.mockRejectedValue({ error: 'some error' });

    expect(await signInSilentCallback(getRoute(), getRoute(), next)).toEqual({ name: 'Root' });
  });
});
