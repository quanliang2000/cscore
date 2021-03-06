﻿using com.csutil.model.immutable;
using com.csutil.ui;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace com.csutil.tests {

    internal static class Task5_CRUD {

        public static async Task ShowIn(ViewStack viewStack) {
            MyModel model = new MyModel(null, ImmutableList<MyUser>.Empty);
            Middleware<MyModel> exampleMiddleware = Middlewares.NewLoggingMiddleware<MyModel>();
            UndoRedoReducer<MyModel> undoLogic = new UndoRedoReducer<MyModel>();
            DataStore<MyModel> store = new DataStore<MyModel>(undoLogic.Wrap(MyReducer), model, exampleMiddleware);

            MyPresenter presenter = new MyPresenter();
            presenter.targetView = viewStack.ShowView("7GUIs_Task5_CRUD");
            await presenter.LoadModelIntoView(store);
        }

        private class MyPresenter : Presenter<DataStore<MyModel>> {

            public GameObject targetView { get; set; }

            private UserListController listUi;
            private InputField filterInput;
            private Button createButton;
            private Button updateButton;
            private Button deleteButton;
            private InputField lastnameInput;
            private InputField surnameInput;

            public Task OnLoad(DataStore<MyModel> store) {
                var map = targetView.GetLinkMap();

                listUi = map.Get<UserListController>("ListUi");
                filterInput = map.Get<InputField>("FilterInput");
                createButton = map.Get<Button>("Create");
                updateButton = map.Get<Button>("Update");
                deleteButton = map.Get<Button>("Delete");
                lastnameInput = map.Get<InputField>("LastnameInput");
                surnameInput = map.Get<InputField>("SurnameInput");

                listUi.OnUserEntryClicked = (clickedUserEntry) => {
                    store.Dispatch(new ChangeSelectionAction() {
                        newSelection = clickedUserEntry
                    });
                };
                listUi.SubscribeToStateChanges(store, model => model.users, delegate { LoadList(store); });
                filterInput.SetOnValueChangedActionThrottled(delegate { LoadList(store); });

                lastnameInput.SubscribeToStateChanges(store, model => model.currentlySelectedUser?.lastname);
                surnameInput.SubscribeToStateChanges(store, model => model.currentlySelectedUser?.surname);

                store.AddStateChangeListener(model => model.currentlySelectedUser, (MyUser selectedUser) => {
                    updateButton.interactable = selectedUser != null;
                    deleteButton.interactable = selectedUser != null;
                }, triggerInstantToInit: true);

                createButton.SetOnClickAction(delegate {
                    store.Dispatch(new CreateAction() {
                        newUser = new MyUser(lastnameInput.text, surnameInput.text)
                    });
                });
                updateButton.SetOnClickAction(delegate {
                    store.Dispatch(new UpdateAction() {
                        userToUpdate = store.GetState().currentlySelectedUser,
                        newUserValues = new MyUser(lastnameInput.text, surnameInput.text)
                    });
                });
                deleteButton.SetOnClickAction(delegate {
                    store.Dispatch(new DeleteAction() {
                        userToDelete = store.GetState().currentlySelectedUser
                    });
                });

                // Add undo of model changes:
                var undoClickedAtLeastOnce = map.Get<Button>("Undo").SetOnClickAction(delegate {
                    store.Dispatch(new UndoAction<MyModel>());
                });

                // The presenter will signal that loading is done once the user clicked undo at least once
                return undoClickedAtLeastOnce; // In this case done to allow auto. testing the presenter
            }

            /// <summary> Takes the list of users, filters it as a prefix based on the 
            /// current filterInput text and shows the result in the UI </summary>
            /// <param name="resetUi"> If this set to true, the list will fully rebuild its UI (this would clear
            /// its ui state, eg the scroll position the user currently scrolled to </param>
            private void LoadList(DataStore<MyModel> store, bool resetUi = false) {
                MyModel myModel = store.GetState();
                var usersThatMatchFilter = myModel.users.Filter(u => u.ToString().StartsWith(filterInput.text));
                listUi.SetListData(usersThatMatchFilter.ToList(), resetUi);
            }

        }

        #region Actions that can be send to the Redux data store

        private class ChangeSelectionAction {
            public MyUser newSelection;
        }

        private class CreateAction {
            public MyUser newUser;
        }

        private class DeleteAction {
            public MyUser userToDelete;
        }

        private class UpdateAction {
            public MyUser userToUpdate;
            public MyUser newUserValues;
        }

        #endregion

        #region Reducers of the Redux data store that will process the actions

        private static MyModel MyReducer(MyModel previousState, object action) {
            bool modelChanged = false;
            var users = previousState.MutateField(previousState.users, action, ReduceUsers, ref modelChanged);
            var selectedUser = previousState.MutateField(previousState.currentlySelectedUser, action, ReduceSelection, ref modelChanged);
            if (modelChanged) { return new MyModel(selectedUser, users); }
            return previousState;
        }

        private static ImmutableList<MyUser> ReduceUsers(MyModel parent, ImmutableList<MyUser> oldUsers, object action) {
            var users = oldUsers.MutateEntries(action, ReduceUser);
            if (action is CreateAction add) { users = users.AddOrCreate(add.newUser); }
            if (action is DeleteAction del) { users = users.Remove(del.userToDelete); }
            return users;
        }

        private static MyUser ReduceSelection(MyModel parent, MyUser oldFieldValue, object action) {
            if (action is ChangeSelectionAction a) { return a.newSelection; }
            if (action is DeleteAction d) {
                AssertV2.AreEqual(oldFieldValue, d.userToDelete, "currentlySelectedUser");
                return null; // Clear selection when deleted
            }
            return ReduceUser(oldFieldValue, action);
        }

        private static MyUser ReduceUser(MyUser oldUser, object action) {
            if (action is UpdateAction a && oldUser.Equals(a.userToUpdate)) { return a.newUserValues; }
            return oldUser;
        }

        #endregion

        #region Model which is immutable and can only be changed through the Redux datastore

        internal class MyModel {

            public readonly MyUser currentlySelectedUser;
            public readonly ImmutableList<MyUser> users;

            public MyModel(MyUser currentlySelectedUser, ImmutableList<MyUser> users) {
                this.currentlySelectedUser = currentlySelectedUser;
                this.users = users;
            }

        }

        internal class MyUser {

            public readonly string lastname;
            public readonly string surname;

            public MyUser(string lastname, string surname) {
                this.lastname = lastname;
                this.surname = surname;
            }

            public override bool Equals(object o) {
                return o is MyUser u && lastname == u.lastname && surname == u.surname;
            }

            public override int GetHashCode() { // Needed because https://stackoverflow.com/a/371348/165106
                int hashCode = -575834488; // Method body is autogenerated by Vistual Studio
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(lastname);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(surname);
                return hashCode;
            }

            public override string ToString() { return lastname + ", " + surname; }

        }

        #endregion

    }

}