﻿using System;
using System.Threading.Tasks;

namespace LetterApp.Core.Services.Interfaces
{
    public interface IDialogService
    {
        void ShowAlert(string title, AlertType alertType, float duration = 3.5f);
        Task<string> ShowTextInput(string title = "", string subtitle = "", string inputContent = "", string confirmButtonText = "", string hint = "", InputType inputType = InputType.Text);
        Task<string> ShowOptions(string title = "", OptionsType optionsType = OptionsType.List, string cancelButtonText = "", params string[] options);
        Task<bool> ShowInformation(string title = "", string text1 = "", string text2 = "",string text3 = "", string confirmButtonText = "");
        Task<bool> ShowQuestion(string title = "", string buttonText = "", QuestionType questionType = QuestionType.Normal);
        void StartLoading();
        void StopLoading();
    }

    public enum AlertType
    {
        Null,
        Success,
        Error,
        Info,
    }

    public enum InputType
    {
        Null,
        Email,
        Text,
        Number,
        Phone
    }

    public enum OptionsType
    {
        Null,
        List,
        Radio
    }

    public enum QuestionType
    {
        Null,
        Good,
        Bad,
        Normal
    }
}
